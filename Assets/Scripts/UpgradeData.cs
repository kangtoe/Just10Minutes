using System.Collections;
using System.Collections.Generic;

public class UpgradeFieldInfo
{
    public string infoText;
    public float currAmount;
    public float nextAmount;

    public UpgradeFieldInfo(string infoText, float currAmount, float nextAmount)
    {
        this.infoText = infoText;
        this.currAmount = currAmount;
        this.nextAmount = nextAmount;
    }
}

public static class UpgradeData
{
    public static int MaxLevel = 5;

    static Dictionary<UpgradeField, float[]> Datas = new()
    {
        { UpgradeField.MaxDurability, new float[6] { 100,    150,     200,    250,    300,    350     } },
        { UpgradeField.MaxShield,     new float[6] { 100,    150,     200,    250,    300,    350     } },
        { UpgradeField.OnImpact,      new float[6] { 10,     15,      20,     25,     30,     35      } },
        { UpgradeField.MultiShot,     new float[6] { 1,      2,       2,      3,      3,      4       } },

        // TODO: 미구현 시스템 (Missle, Pulse)
        // { UpgradeField.Missle,      new float[6] { 0,       2,       3,      4,      5,      6       } },
        // { UpgradeField.ReloadTime,  new float[6] { 2,       2,       1.75f,  1.5f,   1.25f,  1f      } },
        // { UpgradeField.Damage,      new float[6] { 0,       50,      50,     75,     75,     100     } },
        // { UpgradeField.ChargeTime,  new float[6] { 30,      30,      25,     20,     15,     10      } },
    };

    public static UpgradeField[] GetRalatedFields(UpgradeType _type)
    {
        switch (_type)
        {
            case UpgradeType.Ship:
                return new UpgradeField[] { UpgradeField.MaxDurability, UpgradeField.MaxShield, UpgradeField.OnImpact };
            case UpgradeType.Shooter:
                return new UpgradeField[] { UpgradeField.MultiShot };
            case UpgradeType.Missle:
                return new UpgradeField[] { }; // TODO: Missle 시스템 미구현
            case UpgradeType.Pulse:
                return new UpgradeField[] { }; // TODO: Pulse 시스템 미구현

            default:
                return null;
        }
    }

    static T GetSafeElem<T>(T[] arr, int idx)
    {
        if (idx < 0) return arr[0];
        if (idx >= arr.Length) return arr[arr.Length -1];
        return arr[idx];
    }

    static (float, float) GetAmount(UpgradeField _type, int level)
    {
        float[] arr = Datas[_type];

        float curr = GetSafeElem(arr, level);
        float next = GetSafeElem(arr, level + 1);

        return (curr, next);
    }

    public static UpgradeFieldInfo GetFieldInfo(UpgradeField _type, int level)
    {
        UpgradeFieldInfo info;

        var (curr, next) = GetAmount(_type, level);
        info = new UpgradeFieldInfo(_type.ToString(), curr, next);

        return info;
    }

    public static string GetDescString(UpgradeType _type, int level, List<UpgradeFieldInfo> fieldInfos, string colorCode)
    {
        string str = "";
        bool isMaxlevel = level >= MaxLevel;

        if (_type == UpgradeType.EmergencyProtocol)
        {

            //str += "- EFFECT -\n";
            str += $"\nrestore <color=#{colorCode}>ALL</color> status";
            //str += $"\n<color=#{colorCode}>restore</color> shield";
            //str += $"\n<color=#{colorCode}>initialize</color> heat";
            //str += $"\n<color=#{colorCode}>reload</color> missles";
            //str += $"\n<color=#{colorCode}>emit</color> pulse";

        }
        else
        {
            //str += "- UPGRADE -\n";
            foreach (UpgradeFieldInfo fieldInfo in fieldInfos)
            {
                str += "\n" + fieldInfo.infoText + " " + fieldInfo.currAmount;
                if(!isMaxlevel) str += " -> " + $"<color=#{colorCode}>" + fieldInfo.nextAmount + "</color>";
            }
            if (isMaxlevel) str += "\n" + "Max Level";
        }

        return str;
    }

    public static string GetTitleString(UpgradeType _type, int level = 0)
    {
        string str;

        if (_type == UpgradeType.EmergencyProtocol)
        {
            str = UpgradeType.EmergencyProtocol.ToString();
        }
        else
        {
            string levelText = GetRomanNumber(level);
            str = _type.ToString() + "\nLv." + levelText;
        }

        return str;
    }

    static string GetRomanNumber(int level)
    {
        switch (level)
        {
            case 1:
                return "I";
            case 2:
                return "II";
            case 3:
                return "III";
            case 4:
                return "IV";
            case 5:
                return "V";
            default:
                return "-";
        }
    }
}
