using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class CompressAnalyzer
    {
        private readonly CustomSampler _compressSampler = CustomSampler.Create( $"{nameof(CompressAnalyzer)}.{nameof(GetCommonDataTokens)}" );

        public IReadOnlyList<TokenData> GetCommonDataTokens( ReaderBase reader )
        {
            _compressSampler.Begin();
            var propertyNames = new Dictionary<String, TokenData>();
            var stringValues = new Dictionary<String, TokenData>();

            while ( reader.ReadNextToken() != EToken.EoF )
            {
                switch ( reader.CurrentToken )
                {
                    case EToken.PropertyName:
                    {
                        var propertyName = reader.GetPropertyName();
                        if( propertyNames.TryGetValue( propertyName, out var tokenData ) )
                            tokenData.Count++;
                        else
                            propertyNames[propertyName] = new TokenData { Token = EToken.PropertyName, Value = propertyName, Count = 1 };
                    }
                        break;
                    // case EToken.Null:
                    //     break;
                     case EToken.String:
                    {
                        var stringValue = reader.GetStringValue();
                        if ( String.IsNullOrEmpty( stringValue ) )
                            break;
                        if( stringValues.TryGetValue( stringValue, out var tokenData ) )
                            tokenData.Count++;
                        else
                            stringValues[stringValue] = new TokenData { Token = EToken.String, Value = stringValue, Count = 1 };
                    }
                         break;
                    // case EToken.Integer:
                    //     break;
                    // case EToken.Float:
                    //     break;
                    // case EToken.Boolean:
                    //     break;
                    // case EToken.Guid:
                    //     break;
                    // case EToken.Enum:
                    //     break;
                    // case EToken.Type:
                    //     break;
                    // case EToken.EoF:
                    //     break;
                    default:
                        break;
                }
            }

            var result = new List<TokenData>( propertyNames.Count + stringValues.Count );
            result.AddRange( propertyNames.Values );
            result.AddRange( stringValues.Values );
            result.RemoveAll( t => t.Count < 3 );
            result.Sort( ( a, b ) => (b.Value.Length * b.Count).CompareTo( a.Value.Length * a.Count ) );

            _compressSampler.End();
            return result;
        }

        public class TokenData
        {
            public EToken Token;
            public String Value;
            public Int32  Count;
        }
    }
}