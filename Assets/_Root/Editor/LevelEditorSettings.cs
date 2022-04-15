using System;
using System.Collections.Generic;

namespace Pancake.Editor
{
    [Serializable]
    public class LevelEditorSettings
    {
        public List<string> pickupObjectWhiteList;
        public List<string> pickupObjectBlackList;
    }
}