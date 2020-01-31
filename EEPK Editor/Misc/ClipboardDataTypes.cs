namespace EEPK_Organiser.Misc
{
    public static class ClipboardDataTypes
    {
        public static readonly string Effect = string.Format("EEPK_ORGANISER_{0}_EFFECT", GeneralInfo.CurrentVersionString);
        public static readonly string EffectPart = string.Format("EEPK_ORGANISER_{0}_EFFECT_PART", GeneralInfo.CurrentVersionString);
        public static readonly string Asset = string.Format("EEPK_ORGANISER_{0}_ASSET_", GeneralInfo.CurrentVersionString); //+ AssetType
        public static readonly string EmoFile = string.Format("EEPK_ORGANISER_{0}_EMO_FILE", GeneralInfo.CurrentVersionString);
        public static readonly string EmpTextureEntry = string.Format("EEPK_ORGANISER_{0}_EMP_TEXTURE_ENTRY", GeneralInfo.CurrentVersionString);
        public static readonly string EmpParticleEffect = string.Format("EEPK_ORGANISER_{0}_EMP_PARTICLE_EFFECT", GeneralInfo.CurrentVersionString);
        public static readonly string EmbTexture = string.Format("EEPK_ORGANISER_{0}_EMB_TEXTURE", GeneralInfo.CurrentVersionString);
        public static readonly string EmmMaterial = string.Format("EEPK_ORGANISER_{0}_EMM_MATERIAL", GeneralInfo.CurrentVersionString);
        public static readonly string EmmParameter = string.Format("EEPK_ORGANISER_{0}_EMM_PARAMETER", GeneralInfo.CurrentVersionString);
        public static readonly string EmaEntry = string.Format("EEPK_ORGANISER_{0}_EMA_ENTRY", GeneralInfo.CurrentVersionString);
        public static readonly string EtrMainEntry = string.Format("EEPK_ORGANISER_{0}_ETR_ENTRY", GeneralInfo.CurrentVersionString);
        public static readonly string EtrTextureEntry = string.Format("EEPK_ORGANISER_{0}_ETR_TEXTURE_ENTRY", GeneralInfo.CurrentVersionString);
        public static readonly string EcfEntry = string.Format("EEPK_ORGANISER_{0}_ECF_ENTRY", GeneralInfo.CurrentVersionString);
    }
}