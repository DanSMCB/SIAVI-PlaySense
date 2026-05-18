using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity;
using TMPro;

public class UIColourPresetSelector : MonoBehaviour
{
    [Header("Assign the Image you want to recolour (set by 'SelectTarget...' methods)")]
    [SerializeField] private Image targetImage;
    [SerializeField] private MultiHandLandmarkListAnnotation handAnnotation; // For applying colour changes to hand landmarks

    [SerializeField] private bool isLeftHand;
    [SerializeField] private bool isRightHand;

    [SerializeField] private TMP_Text rightText;
    [SerializeField] private TMP_Text leftText;


    // ---------- Colour methods (hook these to each colour preset button) ----------
    public void Colour_Red() => Apply(new Color32(255, 0, 0, 255));
    public void Colour_Orange() => Apply(new Color32(255, 128, 0, 255));
    public void Colour_Yellow() => Apply(new Color32(255, 235, 0, 255));
    public void Colour_Green() => Apply(new Color32(0, 255, 0, 255));
    public void Colour_Cyan() => Apply(new Color32(0, 255, 255, 255));
    public void Colour_Pink() => Apply(new Color32(255, 105, 180, 255));
    public void Colour_DarkRed() => Apply(new Color32(128, 0, 0, 255));
    public void Colour_Blue() => Apply(new Color32(0, 80, 255, 255));
    public void Colour_DarkGreen() => Apply(new Color32(0, 128, 0, 255));
    public void Colour_Purple() => Apply(new Color32(128, 0, 128, 255));
    public void Colour_White() => Apply(new Color32(255, 255, 255, 255));

    // ---------- Core ----------
    private void Apply(Color c)
    {
        if (targetImage == null)
        {
            Debug.LogWarning($"{nameof(UIColourPresetSelector)}: No targetImage selected.");
            return;
        }

        targetImage.color = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(targetImage.color.a * 255));
        if (isRightHand) {
            handAnnotation.SetLeftLandmarkColor(c);
            SetOutlineColor(rightText, c);
        }
        if (isLeftHand) {
            handAnnotation.SetRightLandmarkColor(c);
            SetOutlineColor(leftText, c);
        }
    }

    private static void SetOutlineColor(TMP_Text text, Color outlineColor)
    {
        if (text == null) return;

        var mat = text.fontMaterial;
       
        if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
            mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);

        // Force redraw
        text.SetMaterialDirty();
        text.SetVerticesDirty();
    }
}