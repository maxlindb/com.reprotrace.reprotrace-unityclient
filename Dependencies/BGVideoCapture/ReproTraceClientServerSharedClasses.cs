using System;
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


[System.Serializable]
public class LiveLinkAdvertiseAndGetCommandRequest
{
    public string computerName;
}

[System.Serializable]
public class LiveLinkAdvertiseAndGetCommandResponse
{
    public string error;
    public List<PendingLiveCommand> commandsToRun;
}

[System.Serializable]
public class PendingLiveCommand
{
    public string guid;
    public string forMachine;
    public string forProject;
    public string command;
    public string fileUrl;
    public string fileName;
    public long timeIssuedTicks;
}