using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SceneMenu : MonoBehaviour
{
    public TextMeshProUGUI stepTimeText;
    public Slider stepTimeSlider;
    public Toggle loopToggle;
    public FactoryReplay FactoryReplay;

    private void Start()
    {
        loopToggle.isOn = FindObjectOfType<WorldInfo>().loop;
        float sliderValue = FindObjectOfType<WorldInfo>().step_time;
        stepTimeSlider.value = sliderValue;
        stepTimeText.text = "Step Time: " + sliderValue.ToString() + "s";
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (FindObjectOfType<WorldInfo>().step_time > 0.3)
            {
                ChangeStepTimeBy(-0.2f);
            }
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            if(FindObjectOfType<WorldInfo>().step_time < 3.5)
            {
                ChangeStepTimeBy(0.2f);
            }
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            MainMenuGo();
        }

        else if (Input.GetKeyDown(KeyCode.L))
        {
            loopToggle.isOn =! loopToggle.isOn;
            ChangeLoop();
        }

    }

    public void Restart()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void MainMenuGo()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ChangeStepTime()
    {
        float sliderValue = stepTimeSlider.value;
        sliderValue = Mathf.Round(sliderValue * 10f) / 10f;
        FindObjectOfType<WorldInfo>().step_time = sliderValue;
        stepTimeText.text = "Step Time: " + sliderValue.ToString() + "s";
        FactoryReplay.stepTime = sliderValue;
    }

    public void ChangeStepTimeBy(float newvalue)
    {
        float sliderValue = FindObjectOfType<WorldInfo>().step_time +newvalue;
        sliderValue = Mathf.Round(sliderValue * 10f) / 10f;
        stepTimeSlider.value = sliderValue;
        FindObjectOfType<WorldInfo>().step_time = sliderValue;
        stepTimeText.text = "Step Time: " + sliderValue.ToString() + "s";
        FactoryReplay.stepTime = sliderValue;
    }

    public void ChangeLoop()
    {
        FindObjectOfType<WorldInfo>().loop = loopToggle.isOn;
        FactoryReplay.loop = loopToggle.isOn;
    }
}
