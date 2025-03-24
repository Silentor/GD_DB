using GDDB;
using UnityEngine;

namespace Client
{
    public class TestPropertyDrawer : MonoBehaviour
    {
        [GdObjectFilter]
        public GdFolderRef         FolderRefFilter;
        public GdFolderRef         FolderRefNoFilter;
    }
}
