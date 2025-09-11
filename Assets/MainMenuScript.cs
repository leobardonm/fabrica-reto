using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  // needed to load scenes
using TMPro;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TextMeshProUGUI stepTimeText;
    public Slider stepTimeSlider;
    public Toggle loopToggle;

    public void Update()
    {
        if (Input.GetKey(KeyCode.S)) SceneManager.LoadScene("SampleScene");
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void ChangeStepTime()
    {
        float sliderValue = stepTimeSlider.value;
        sliderValue = Mathf.Round(sliderValue * 10f) / 10f;
        FindObjectOfType<WorldInfo>().step_time = sliderValue;
    }

    public void UpdateStepTimeText()
    {
        float sliderValue = stepTimeSlider.value;
        sliderValue = Mathf.Round(sliderValue * 10f) / 10f;

        stepTimeText.text = "Step Time: " + sliderValue.ToString() + "s";
    }

    public void ChangeLoop()
    {
        FindObjectOfType<WorldInfo>().loop = loopToggle.isOn;
    }

    // (Optional) add a quit button
    public void QuitGame()
    {
        Debug.Log("Quit pressed!"); // won’t quit inside editor
        Application.Quit();
    }
}
