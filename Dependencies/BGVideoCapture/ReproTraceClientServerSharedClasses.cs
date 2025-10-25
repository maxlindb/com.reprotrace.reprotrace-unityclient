using System.Collections;
using System.Collections.Generic;

public class UploadResponse
{
    public bool success;
    public string message;

    public ObtainedProjectConfiguration projectConfiguration;
}

[System.Serializable]
public class ObtainedProjectConfiguration
{
    public string projectName;
    public bool projectHasTrelloEnabled;
}