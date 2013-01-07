using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

#region SettingTag
public class SettingTag
{
    public const string SettingsTag = "Settings";
    public const string SetTag = "Setting";
    public const string ConfidenceValue = "ConfidenceValue";
    public const string PictureSize = "PictureSize";
    public const string Language = "Language";
    public const string CreateMaxTime = "CreateMaxTime";
    public const string CreateMinTime = "CreateMinTime";
    public const string MaxDownSpeed = "MaxDownSpeed";
    public const string MinDownSpeed = "MinDownSpeed";
    public const string SuccessSound = "SuccessSound";
    public const string FailSound = "FailSound";
}
#endregion

#region SettingData Class
public class SettingData
{
    public string ConfidenceValue;
    public string PictureSize;
    public string Language;
    public string CreateMaxTime;
    public string CreateMinTime;
    public string MaxDownSpeed;
    public string MinDownSpeed;
    public string SuccessSound;
    public string FailSound;

    public SettingData()
    {

    }

    public SettingData(string confidenceValue, string pictureSize, string language, string createMaxTime, string createMinTime, string maxDownSpeed, string minDownSpeed, string successSound, string failSound)
    {
        this.ConfidenceValue = confidenceValue;
        this.PictureSize = pictureSize;
        this.Language = language;
        this.CreateMaxTime = createMaxTime;
        this.CreateMinTime = createMinTime;
        this.MaxDownSpeed = maxDownSpeed;
        this.MinDownSpeed = minDownSpeed;
        this.SuccessSound = successSound;
        this.FailSound = failSound;
    }
}
#endregion

#region FileManager class

/// <summary>
/// File manager, support read and write score data and config to XML file format.
/// </summary>
public class FileManager
{
    // Save exception message
    public Exception Ex { get; private set; }

    // Store all username's score
    private SettingData settingData;

    // Filename for reading
    private string fileName;


    public FileManager()
    {
        this.fileName = string.Empty;
        this.Ex = null;
    }

    public void ConfigReader(string filename)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreWhitespace = true;
        settings.ValidationType = ValidationType.None;
        XmlReader reader = XmlTextReader.Create(filename, settings);

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    string tagName = reader.LocalName;
                    if (tagName.Equals(SettingTag.SetTag))
                    {

                        this.settingData = new SettingData(reader[SettingTag.ConfidenceValue],
                                                           reader[SettingTag.PictureSize],
                                                           reader[SettingTag.Language],
                                                           reader[SettingTag.CreateMaxTime],
                                                           reader[SettingTag.CreateMinTime],
                                                           reader[SettingTag.MaxDownSpeed],
                                                           reader[SettingTag.MinDownSpeed],
                                                           reader[SettingTag.SuccessSound],
                                                           reader[SettingTag.FailSound]);


                    }
                    break;
                default:
                    break;
            }
        }
        reader.Close();
    }

    public void ConfigWrite(SettingData setting)
    {
        this.settingData = setting;
        XmlDocument doc = new XmlDocument();
        doc.Load(GameDefinition.SettingFilePath);
        XmlNode setNode = doc.SelectSingleNode(SettingTag.SettingsTag + "/" + SettingTag.SetTag);
        XmlElement element = (XmlElement)setNode;
        XmlAttributeCollection attributes = element.Attributes;
        foreach (XmlAttribute attribute in attributes)
        { 
            switch (attribute.Name)
            {
                case SettingTag.ConfidenceValue:
                    attribute.Value = this.settingData.ConfidenceValue;
                    break;
                case SettingTag.PictureSize:
                    attribute.Value = this.settingData.PictureSize;
                    break;
                case SettingTag.Language:
                    attribute.Value = this.settingData.Language;
                    break;
                case SettingTag.CreateMaxTime:
                    attribute.Value = this.settingData.CreateMaxTime;
                    break;
                case SettingTag.CreateMinTime:
                    attribute.Value = this.settingData.CreateMinTime;
                    break;
                case SettingTag.MaxDownSpeed:
                    attribute.Value = this.settingData.MaxDownSpeed;
                    break;
                case SettingTag.MinDownSpeed:
                    attribute.Value = this.settingData.MinDownSpeed;
                    break;
                case SettingTag.SuccessSound:
                    attribute.Value = this.settingData.SuccessSound;
                    break;
                case SettingTag.FailSound:
                    attribute.Value = this.settingData.FailSound;
                    break;
                default:
                    break;
            }
        }
        doc.Save(GameDefinition.SettingFilePath);
    }

    public SettingData GetSettingData()
    {
        return this.settingData;
    }

    public bool VerifyFileExist(string filename)
    {
        XmlDocument document = new XmlDocument();
        switch (filename)
        {
            case GameDefinition.SettingFilePath:
                
                if (!File.Exists(filename))
                {
                    XmlNode docNode = document.CreateXmlDeclaration("1.0", "UTF-8", null);
                    document.AppendChild(docNode);
                    XmlNode productsNode = document.CreateElement(SettingTag.SettingsTag);
                    document.AppendChild(productsNode);
                    document.Save(filename);
                    XmlNode nodee = document.SelectSingleNode(SettingTag.SettingsTag);
                    XmlElement newMachine = document.CreateElement(SettingTag.SetTag);
                    newMachine.SetAttribute(SettingTag.ConfidenceValue, "0.6");
                    newMachine.SetAttribute(SettingTag.PictureSize, "100");
                    newMachine.SetAttribute(SettingTag.Language, "en-US");
                    newMachine.SetAttribute(SettingTag.CreateMaxTime, "6");
                    newMachine.SetAttribute(SettingTag.CreateMinTime, "4");
                    newMachine.SetAttribute(SettingTag.MaxDownSpeed, "3");
                    newMachine.SetAttribute(SettingTag.MinDownSpeed, "1");
                    newMachine.SetAttribute(SettingTag.SuccessSound, "Success.mp3");
                    newMachine.SetAttribute(SettingTag.FailSound, "Fail.mp3");
                    nodee.AppendChild(newMachine);
                    document.Save(filename);
                    this.settingData = new SettingData("0.6", "100", "en-US", "6", "4", "3", "1", "Success.mp3", "Fail.mp3");
                    return false;
                }
                break;

            default:
                break;
        }
        return true;
    }
}

#endregion
