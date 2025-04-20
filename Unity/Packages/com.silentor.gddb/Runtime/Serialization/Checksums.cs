using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Gddb.Serialization
{
    public static class Checksums
    {
        public static ulong ComputeFletcher64( String input )
        {
            if ( String.IsNullOrEmpty( input ) )
                return 0;

            var bytes = MemoryMarshal.Cast<char, byte>( input.AsSpan() );
            return ComputeFletcher64( bytes );
        }

        //https://gist.github.com/regularcoder/8254723?permalink_comment_id=5366799#gistcomment-5366799
        private static ulong ComputeFletcher64( byte[] input)
        {
            // Convert the input data into an array of 32bit blocks.
            int blockCount = Math.DivRem(input.Length, sizeof(uint), out int rem) + (rem > 0 ? 1 : 0);
            var blocks = ArrayPool<uint>.Shared.Rent( blockCount );
            Buffer.BlockCopy(input, 0, blocks, 0, input.Length);

            // Use 2^32 − 1 as the modulus.
            const ulong mod = uint.MaxValue;
            ulong sum1 = 0;
            ulong sum2 = 0;
            foreach (ulong block in blocks)
            {
                sum1 = (sum1 + block) % mod;
                sum2 = (sum2 + sum1)  % mod;
            }

            ArrayPool<uint>.Shared.Return( blocks );

            // Combine checksums.
            return (sum2 << 32) | sum1;
        }

        //https://gist.github.com/regularcoder/8254723?permalink_comment_id=5366799#gistcomment-5366799
        public static ulong ComputeFletcher64( ReadOnlySpan<byte> input )
        {
            // Use 2^32 − 1 as the modulus.
            const ulong mod  = uint.MaxValue;
            ulong       sum1 = 0;
            ulong       sum2 = 0;

            if ( input.Length % (sizeof(uint)) != 0 )                              //Slow pat, copy data
            {
                //Prepare buffer
                int blockCount = Math.DivRem(input.Length, sizeof(uint), out int rem) + (rem > 0 ? 1 : 0);
                Span<uint> buffer = stackalloc uint[blockCount];
                input.CopyTo( MemoryMarshal.Cast<uint, byte>(buffer) );
                foreach (ulong block in buffer)
                {
                    sum1 = (sum1 + block) % mod;
                    sum2 = (sum2 + sum1)  % mod;
                }
            }
            else                                                                    //Fast path, no copy
            {
                var buffer = MemoryMarshal.Cast<byte, uint>( input );
                foreach (ulong block in buffer)
                {
                    sum1 = (sum1 + block) % mod;
                    sum2 = (sum2 + sum1)  % mod;
                }
            }

            // Combine checksums.
            return (sum2 << 32) | sum1;
        }

        /// <summary>
        /// Calculate checksum of folders structure (ignore objects)
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static ulong GetFoldersChecksum( this GdFolder folder )
        {
            var      result   = 0ul;
            foreach ( var f in folder.EnumerateFoldersDFS(  ) )
            {
                unchecked
                {
                    var folderHash = (UInt64)f.FolderGuid.GetHashCode() + ComputeFletcher64( f.GetPath() )/* + (UInt64)Objects.Count*/;
                    result += folderHash;
                }
            }

            return result;
        }

        public static ulong GetGuidChecksum( this Guid guid )
        {
            Span<byte> byteBuffer = stackalloc byte[16];
            guid.TryWriteBytes( byteBuffer );
            var ulongBuffer = MemoryMarshal.Cast<byte, ulong>( byteBuffer );
            return ulongBuffer[ 0 ] ^ ulongBuffer[ 1 ];
        }


    }
}