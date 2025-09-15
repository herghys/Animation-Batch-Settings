using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Herghys.AnimationBatchClipHelper.Saves
{
    public class GlobalSavesData
    {
        public const string DefaultHelperLocation = "Animation Helper";

        public const string DefaultAnimationClipBatchFileName = "AnimationClipBatchData.json";
        public const string DefaultAnimationClipBatchLocation = "Animation Clip Batch";
        public static string AnimationClipBatchLocation => Path.Combine(Application.dataPath, "EditorResources", DefaultHelperLocation, DefaultAnimationClipBatchLocation);


        public const string DefaultAnimationModelBatchFileName = "AnimationModelClipBatchData.json";
        public const string DefaultAnimtionModelBatchLocation = "Model Clip Batch";

        public static string AnimationModelClipBatchLocation => Path.Combine(Application.dataPath, "EditorResources", DefaultHelperLocation, DefaultAnimtionModelBatchLocation);
    }
}
