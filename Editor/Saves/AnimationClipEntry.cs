using System;
using System.Collections.Generic;

namespace Herghys.AnimationBatchClipHelper.Saves
{
    [Serializable]
    public class AnimationClipEntry
    {
        public string GUID;
        public string ClipName;
        public bool Status;

        public AnimationClipEntry() { }

        public AnimationClipEntry(string guid, string name, bool status = true)
        {
            GUID = guid;
            ClipName = name;
            Status = status;
        }
    }

    [System.Serializable]
    public class AnimationModelClipBatch
    {
        public string ModelGUID;
        public string ModelName;
        public List<AnimationClipEntry> Clips = new();

        public AnimationModelClipBatch() { }

        public AnimationModelClipBatch(string guid, string name)
        {
            ModelGUID = guid;
            ModelName = name;
        }
    }
}
