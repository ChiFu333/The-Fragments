using UnityEngine;

public static partial class R
{
    public static bool isInited = false;
    
    public static Material materialGameObjectLit;
    public static Material materialUILit;
    public static Material materialFade;

    public static void InitAll()
    {
        isInited = true;

        R.InitAudio();

        materialGameObjectLit = Resources.Load<Material>("BuildMaterials/MaterialGameObjectLit");
        materialUILit = Resources.Load<Material>("BuildMaterials/MaterialUILit");
        materialFade = Resources.Load<Material>("BuildMaterials/MaterialFade");
    }
}
