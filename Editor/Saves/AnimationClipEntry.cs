using System;
using System.Collections.Generic;

namespace Herghys.AnimationBatchClipHelper.Saves
{


    [Serializable]
    public class AnimationClipEntry
    {
        public string ClipName;
        public bool Status;

        public AnimationClipEntry() { }

        public AnimationClipEntry(string name, bool status = true)
        {
            ClipName = name;
            Status = status;
        }
    }

    [Serializable]
    public class GUIDAnimationClipEntry : AnimationClipEntry
    {
        public string GUID;
        public GUIDAnimationClipEntry() { }

        public GUIDAnimationClipEntry(string guid, string name, bool status = true) : base(name, status)
        {
            GUID = guid;
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
