using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Skill
{
    public string name;
    public int level;

    public Skill(string _name, int _level)
    {
        name = _name;
        level = _level;
    }
}

public class SkillBox : MonoBehaviour
{
    [SerializeField] TMP_InputField SkillName;
    [SerializeField] Slider SkillLevelSlider;
    [SerializeField] TMP_Text SkillLevelText;

    public Skill ReturnClass()
    {
        return new Skill(SkillName.text, (int)SkillLevelSlider.value);
    }

    public void SetUI(Skill sk)
    {
        SkillName.text = sk.name;
        SkillLevelSlider.value = sk.level;
    }

    public void SliderChangeUpdate(float num)
    {
        SkillLevelText.text = SkillLevelSlider.value.ToString();
    }
}
