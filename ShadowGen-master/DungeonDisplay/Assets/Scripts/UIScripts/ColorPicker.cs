using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [SerializeField]
    private Image previewImage = null;
    [SerializeField]
    private Slider hueSlider = null;
    [SerializeField]
    private Slider saturationSlider = null;
    [SerializeField]
    private Slider valueSlider = null;
    [SerializeField]
    private Slider alphaSlider = null;

    private Button selectedButton;
    private Color color;

    private void Awake()
    {
        color = Color.HSVToRGB(1, 1, 1);
        Reset();
    }

    /// <summary>
    /// Resets slider and colors to default values
    /// </summary>
    private void Reset()
    {
        alphaSlider.value = color.a;

        float colorH;
        float colorV;
        float colorS;
        Color.RGBToHSV(color, out colorH, out colorS, out colorV);
        hueSlider.value = colorH;
        saturationSlider.value = colorS;
        valueSlider.value = colorV;

        UpdatePreview();
    }

    /// <summary>
    /// Opens the window and sets the selected button
    /// </summary>
    /// <param name="button"></param>
    public void SelectButton(Button button)
    {
        selectedButton = button;
        color = selectedButton.colors.normalColor;
        Reset();

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Finalizes the color and hides the window
    /// </summary>
    public void OnClose()
    {
        ColorBlock block = selectedButton.colors;
        block.normalColor = color;
        selectedButton.colors = block;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets the red channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetRedChannel(float value)
    {
        color.r = value;
        UpdatePreview();
    }

    /// <summary>
    /// Sets the red channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetGreenChannel(float value)
    {
        color.g = value;
        UpdatePreview();
    }

    /// <summary>
    /// Sets the red channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetBlueChannel(float value)
    {
        color.b = value;
        UpdatePreview();
    }

    /// <summary>
    /// Sets the hue channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SeHueChannel(float value)
    {
        float colorH;
        float colorV;
        float colorS;
        Color.RGBToHSV(color, out colorH, out colorS, out colorV);
        color = Color.HSVToRGB(value,colorS,colorV);
        UpdatePreview();
    }

    /// <summary>
    /// Sets the saturation channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetSaturationChannel(float value)
    {
        float colorH;
        float colorV;
        float colorS;
        Color.RGBToHSV(color, out colorH, out colorS, out colorV);
        color = Color.HSVToRGB(colorH, value, colorV);
        UpdatePreview();
    }

    /// <summary>
    /// Sets the value channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetValueChannel(float value)
    {
        float colorH;
        float colorV;
        float colorS;
        Color.RGBToHSV(color, out colorH, out colorS, out colorV);
        color = Color.HSVToRGB(colorH, colorS, value);
        UpdatePreview();
    }

    /// <summary>
    /// Sets the red channel to the value of the slider
    /// </summary>
    /// <param name="value">The value to set the channel to</param>
    public void SetAlphaChannel(float value)
    {
        color.a = value;
        UpdatePreview();
    }

    /// <summary>
    /// Updates the color of the preview image
    /// </summary>
    public void UpdatePreview()
    {
        previewImage.color = color;
    }
}
