# INSTALLATION

Installation is currently available only using UPM or Direct Download (Clone)

## Install via UPM

### Using Package Manager UI (Reommended)
1. Click Window > Package Manager to open Package Manager UI.
2. Click `+ > Add package from git URL...` and input the repository URL: `https://github.com/herghys/Animation-Batch-Settings.git`
3. Unity will automatically fetch and install the package.

___
### Using Manual Add To Manifest
Add it manually to your `Packages/manifest.json`:

```json
{
    "dependencies": 
    {
        "com.herghys.animation-batch-settings": "https://github.com/herghysAnimation-Batch-Settings.git"
    }
}
```


## Install via Clone Project
### 📂 Install via Clone (Manual)

1. Navigate to your Unity project folder.  
2. Inside the `Packages/` directory, clone this repository:

### 💻Bash / Terminal
```bash
#
# Navigate to your Unity project folder
cd YourUnityProject/Packages

# Clone the repository directly into Packages
git clone https://github.com/herghys/Animation-Batch-Settings.git
```