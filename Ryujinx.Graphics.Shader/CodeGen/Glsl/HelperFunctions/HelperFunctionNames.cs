namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class HelperFunctionNames
    {
        public static string AtomicMaxS32 = "Helper_AtomicMaxS32";
        public static string AtomicMinS32 = "Helper_AtomicMinS32";

        public static string MultiplyHighS32 = "Helper_MultiplyHighS32";
        public static string MultiplyHighU32 = "Helper_MultiplyHighU32";

        public static string Shuffle     = "Helper_Shuffle";
        public static string ShuffleDown = "Helper_ShuffleDown";
        public static string ShuffleUp   = "Helper_ShuffleUp";
        public static string ShuffleXor  = "Helper_ShuffleXor";
        public static string SwizzleAdd  = "Helper_SwizzleAdd";

        public static string StoreShared16  = "Helper_StoreShared16";
        public static string StoreShared8   = "Helper_StoreShared8";
        public static string StoreStorage16 = "Helper_StoreStorage16";
        public static string StoreStorage8  = "Helper_StoreStorage8";

        public static string TranslateAddress = "Helper_TranslateAddress";
    }
}