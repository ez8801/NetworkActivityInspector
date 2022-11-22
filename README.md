# Network Activity Inspector
This is a network activity inspector for Unity.

## Installation

### Install via git URL

To install this package, you need to edit your Unity project's `Packages/manifest.json` and add this repository as a dependency. 
``` json
{
  "dependencies": {
    "com.ez.network-activity-inspector": "https://github.com/ez8801/NetworkActivityInspector",
  }
}
```

## Usage

```CSharp
using UnityEngine.Networking;
...
var req = UnityWebRequest.Post("http://helloworld.com");
var operation = req.SendWebRequest();
await EZ.Network.NetworkActivity.OnRequest(operation, request.Param);
...
```
