
private var frameNumber = 0;
private var isRecording = false;

private var sceneName : String = "";

function Start()
{
	sceneName = EditorApplication.currentScene;

	var path : String [] = EditorApplication.currentScene.Split(char.Parse("/"));
    
    path = path[path.Length-1].Split(char.Parse("."));
    
    sceneName = path[0];
    
    Debug.Log("Scene name = " + sceneName);
    
    System.IO.Directory.CreateDirectory("\\Results\\"+sceneName);
    
    Screen.SetResolution(1080,720,true,60);
    
}

function Update () 
{
	

	if (Input.GetKeyDown(KeyCode.R))
	{
		isRecording = !isRecording;
		frameNumber = 0;
		Debug.Log("Recording is "+isRecording);
	}
	
	if (isRecording)
	{
		Time.captureFramerate = 60; // ASN DO I NEED THIS?		
		var filename = ".\\Results\\"+sceneName+"-screenshot"+PadDigits(frameNumber, 6)+".png";		
		Application.CaptureScreenshot(filename);
		frameNumber++;
	}
	else
	{
		Time.captureFramerate = 0;
	}	
}

function PadDigits(n, totalDigits) 
{ 
	nstr = ""+n;
    var pd = ''; 
    if (totalDigits > nstr.length) 
    { 
        for (i=0; i < (totalDigits-nstr.length); i++) 
        { 
            pd += '0'; 
        } 
    } 
    return pd + nstr; 
} 
