using System.Collections;
using System.Collections.Generic;
using GameData.PgcData;
using UnityEngine;

public class AvatarStudioConfig
{
    public class DraftConfig
    {
        public string path;
        public AvatarSubType type;
    }
    
    public class UgcAnimConfig
    {
        public string path;
        public EmoteSubType type;
    }
    
    public static List<DraftConfig> avatarRtConfig = new()
    {
        new(){path = "ic_all", type = AvatarSubType.All},
        new(){path = "ic_gashapon", type = AvatarSubType.Bundle},
        new(){path = "ic_outfits", type = AvatarSubType.Clothes},
        new(){path = "ic_hair", type = AvatarSubType.Hair},
        new(){path = "ic_headwear", type = AvatarSubType.Hats},
        new(){path = "ic_eyes", type = AvatarSubType.Eyes},
        new(){path = "ic_shoes", type = AvatarSubType.Shoe},
        new(){path = "ic_mouth", type = AvatarSubType.Mouth},
        new(){path = "ic_glasses", type = AvatarSubType.Glasses},
        new(){path = "ic_backpack", type = AvatarSubType.Backpack},
        new(){path = "ic_hand", type = AvatarSubType.Hand},
        new(){path = "ic_patterns", type = AvatarSubType.FacePaint},
    };

    public static List<DraftConfig> rtConfigPublish = new()
    {
        new() { path = "ic_all", type = AvatarSubType.All },
        new() { path = "ic_gashapon", type = AvatarSubType.Bundle },
        new() { path = "ic_outfits", type = AvatarSubType.Clothes },
        new() { path = "ic_hair", type = AvatarSubType.Hair },
        new() { path = "ic_headwear", type = AvatarSubType.Hats },
        new() { path = "ic_eyes", type = AvatarSubType.Eyes },
        new() { path = "ic_shoes", type = AvatarSubType.Shoe },
        new() { path = "ic_mouth", type = AvatarSubType.Mouth },
        new() { path = "ic_glasses", type = AvatarSubType.Glasses },
        new() { path = "ic_backpack", type = AvatarSubType.Backpack },
        new() { path = "ic_hand", type = AvatarSubType.Hand },
        new() { path = "ic_patterns", type = AvatarSubType.FacePaint },
    };
    public static List<DraftConfig> petRtConfig = new()
    {
        new(){path = "ic_all", type = AvatarSubType.All},
        new(){path = "ic_skin_pet", type = AvatarSubType.Skin},
        new(){path = "ic_gashapon_pet", type = AvatarSubType.Bundle},
        new(){path = "ic_outfits_pet", type = AvatarSubType.Clothes},
        new(){path = "ic_ear_pet", type = AvatarSubType.Ear},
        new(){path = "ic_hair_pet", type = AvatarSubType.Hair},
        new(){path = "ic_headwear_pet", type = AvatarSubType.Hats},
        new(){path = "ic_scraf_pet", type = AvatarSubType.Scarf},
        new(){path = "ic_glasses_pet", type = AvatarSubType.Glasses},
        new(){path = "ic_eyes_pet", type = AvatarSubType.Eyes},
        new(){path = "ic_mouth_pet", type = AvatarSubType.Mouth},
        new(){path = "ic_patterns_pet", type = AvatarSubType.FacePaint},
        new(){path = "ic_tail_pet", type = AvatarSubType.Tail},
        new(){path = "ic_backpack_pet", type = AvatarSubType.Backpack},
        new(){path = "ic_shoes_pet", type = AvatarSubType.Shoe},
    };
}
