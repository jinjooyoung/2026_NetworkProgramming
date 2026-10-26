using UnityEngine;
using UnityEngine.UI;

public class TimeSlider : MonoBehaviour
{
    public Slider timeSlider;
    public Image fillImage;

    private enum SliderState { Normal, Half, Low }
    private SliderState currentState = SliderState.Normal;

    private readonly Color32 redColor = new Color32(0xF4, 0x43, 0x36, 0xFF);
    private readonly Color32 yellowColor = new Color32(0xFE, 0xCB, 0x10, 0xFF);

    public void Update()
    {
        if (timeSlider.maxValue <= 0) return; // 0으로 나누기 방지

        float ratio = timeSlider.value / timeSlider.maxValue;

        if (ratio <= 0.1666667f)
        {
            if (currentState != SliderState.Low)
            {
                currentState = SliderState.Low;
                fillImage.color = redColor;
            }
        }
        else if (ratio <= 0.5f)
        {
            if (currentState != SliderState.Half)
            {
                currentState = SliderState.Half;
                fillImage.color = yellowColor;
            }
        }
    }

    public void SetUpSlider(float maxValue)
    {
        timeSlider.maxValue = maxValue;
        timeSlider.value = maxValue;
    }

    public void UpdateSlider(float amount)
    {
        timeSlider.value = amount;
    }
}
