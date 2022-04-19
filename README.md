# What

Provive easy way create level design

# How To Install


For version 1.1.1 

Add following line

```csharp
"com.pancake.level-editor": "https://github.com/pancake-llc/level-editor.git?path=Assets/_Root#1.1.1",
"com.pancake.common": "https://github.com/pancake-llc/common.git?path=Assets/_Root#1.1.5",
```

To `Packages/manifest.json`

## Usage

![image](https://user-images.githubusercontent.com/44673303/163957286-6714b6bc-68f5-46b6-9c9e-c3c7a2e1255b.png) 
![image](https://user-images.githubusercontent.com/44673303/163957353-c9b508ef-3425-4625-96a3-1ba3e09f319c.png)


#### _DROP AREA_

1. White List : Contains a list of links to list all the prefabs you can choose from in the PickUp Area
2. Black List : Contains a list of links to list all prefabs that won't show up in the PickUp Area
3. Using `Right Click` to clear all `White List` or `Black List`


### _SETTING_

1. Where Spawn :
   1. Default : 
      1. New instantiate object will spawn in root prefab when you in prefab mode
      2. New instantiate object will spawn in world space when you in scene mode
   
   2. Custom: You can choose to use the object as the root to spawn a new object here


### _PICKUP AREA_

![image](https://user-images.githubusercontent.com/44673303/163959317-78c6f079-69ee-4bb2-b476-21d8e9f7ce3e.png)

Where you choose the object to spawn

+ Using `Shift + Click` to instantiate object
+ Using `Right Click` in item to ping object prefab
+ Using `Right Click` in header Pickup Area to refresh draw collection item pickup
  ![image](https://user-images.githubusercontent.com/44673303/163969707-bc0beca6-2952-414f-8732-e1e4bcbaa630.png)