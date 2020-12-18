SHADOW GEN 

Created by Justin Denis, Alex Herman, and Weston Marshall ©2020
Unity package for dynamically created 2d area lighting
--------------------------------------

-- Importing the Package --	
1) Open your project.
2) Go to Assets > Import Package > Custom Package.
3) Choose ShadowGen.unitypackage from the file browser.
4) Make sure all of the files are selected and click Import.
	
Now all of the files should appear in the project view under the ShadowGen folder.
In order for the assets to work, you need to make sure Lightweight Render Pipeline is
properly set up.

!! TO DO !!

Finally, make sure to enable gizmos in your scene so that you can see the shapes as you
are working with them.

-- Setting up the Scene --
1) Navigate to ShadowGen > Prefabs.
2) In your scene replace the Main Camera with the Main Camera prefab.
3) Add the Shadow and Shadow Manager prefabs to your scene. 
4) Add PointLight, Circle, Rectangle, and CompleShape to your scene in order to build it out

-- Using the Shapes and Lights --
PointLight
	Point lights are the sources of light in your scene.
	Variables 
		!! TO DO !!
	Buttons 
		!! TO DO !!
	
ComplexShape
	ComplexShapes are for any non-circular, non-rectangular shapes
	Variables
		Points - A list of points in the shape. Adjusting the size of the array will let you
			add new points to the shape.
	Buttons 
		Edit Shape - Allows you to manipulate each point in the shape.
		Move Points to Center - ?
		Generate Bounding Sphere - Recreates the bounding sphere of the shape.
			Note: This is usually done automatically. The button is only needed when that fails.
		Spherize Shape - Pushes all of the points out to the same distance from the center to
			the shape and evenly distributes them.
		
Circle
	Circle is a more optimized blocking volume for circular shapes.
	Variables
		Radius - The radius of the circle. This can be adjusted to change the circle's size.
		Resolution - The number of points that make up the circular shape
	Buttons
		Update Shape - Updates the gizmo drawings of the shape to match its current position.
		Edit Shape - Creates handles that can be used to adjust the radius of the circle.
		
Rectangle
	Rectangles are a more optimized blocking volume for rectangular shapes.
	Variables
		Extents - The distance from the center that the edges are. X adjusts the width of the
		rectangle and Y adjusts the height of the rectangle.
	Buttons
		Update Shape - Updates the gizmo drawings of the shape to match its current position.
		Edit Shape - Creates handles that can be used to adjust the extents of the rectangle.