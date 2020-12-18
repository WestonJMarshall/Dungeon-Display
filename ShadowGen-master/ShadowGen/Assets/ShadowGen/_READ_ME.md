# SHADOW GEN 

	Created by Justin Denis, Alex Herman, and Weston Marshall ©2020
	Unity package for dynamically created 2d area lighting
---

## Setting Up Your Project 
### Importing the Package
1. Open your project.
2. Go to Assets > Import Package > Custom Package.
3. Choose ShadowGen.unitypackage from the file browser.
4. Make sure all of the files are selected and click Import.
	
Now all of the files should appear in the project view under the ShadowGen folder.

### Importing Lightweight Render Pipeline
In order for the assets to work, you need to make sure Lightweight Render Pipeline is
properly set up.

1. In your project, go to Window > Package Manager.
2. In the Package Manager, select the Lightweight RP and click Install.
3. In the project folder, go to ShadowGen/LWRP and find the 'LightweightRenderPipelineAsset'.
4. Now go to Edit > Project Settings.
5. In Project Settings, go to the Graphics tab and find the Scriptable Render Pipeline Settings section.
6. Drag the 'LightweightRenderPipelineAsset' into the Scriptable Render Pipeline Settings. 

Finally, make sure to enable gizmos in your scene so that you can see the shapes as you
are working with them.

## Setting up the Scene 
1. Navigate to ShadowGen > Prefabs.
2. In your scene replace the Main Camera with the Main Camera prefab.
3. Add the Shadow and Shadow Manager prefabs to your scene. 
4. Add PointLight, Circle, Rectangle, and ComplexShape to your scene in order to build it out

## Using the Shapes and Lights 
### PointLight
Point lights are the sources of light in your scene.
Variables 
- Points: A list of points in the area of the shape.
Buttons 
- '+' and '-': Add and remove points from the light's shape.
- Edit Light: Allows you to move individual points of the light's shape.
- Generate Bounding Sphere: Recreates the bounding sphere of the shape. 
**Note:** This is usually done automatically. The button is only needed when that fails.
- Spherize Light: Pushes all of the points out to make the shape as close to a circle as it can be.
	
### ComplexShape
ComplexShapes are for any non-circular, non-rectangular shapes.  

**Variables**   
- Points: A list of points in the area of the shape.  

**Buttons**  
- '+' and '-': Add and remove points from the light's shape.
- Edit Shape: Allows you to move individual points of the shape.
- Generate Bounding Sphere:  Recreates the bounding sphere of the shape. 
**Note:** This is usually done automatically. The button is only needed when that fails.
- Spherize Shape: Pushes all of the points out to make the shape as close to a circle as it can be.
		
### Circle
Circle is a more optimized blocking volume for circular shapes.  

**Variables**  
- Radius: The radius of the circle. This can be adjusted to change the circle's size.
- Resolution: The number of points that make up the circular shape  

**Buttons**  
- Update Shape: Updates the gizmo drawings of the shape to match its current position.
- Edit Shape: Creates handles that can be used to adjust the radius of the circle.
		
### Rectangle
Rectangles are a more optimized blocking volume for rectangular shapes.  

**Variables**  
- Extents: The distance from the center that the edges are. X adjusts the width of the rectangle and Y adjusts the height of the rectangle.  

**Buttons**  
- Update Shape: Updates the gizmo drawings of the shape to match its current position.
- Edit Shape: Creates handles that can be used to adjust the extents of the rectangle.