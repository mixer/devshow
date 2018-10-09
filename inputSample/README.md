# Input Sample

This sample shows how you can use vanilla JS with third party libraries like JQuery to poll for gamepad input and visualize it on a controller image.

It is not a good example for how to visualize the controller and its buttons as the images are brittle and not resilient to different layouts or resolutions.

To run the sample on your channel:
 - npm i in customControls
 - npm i in gameClient
 - Open customControls folder in the CDK
 - Upload the controls to your channel
 - Open gameClient\mixerauth.json
 - Fill out the clientId, clientSecret, and versionId with your developer information
 - npm start from \gameClient

# Game Client
The game client does next to nothing in this project. It exists only so that we can let Mixer know when we want to run the custom control on our channel. It's a clone of the Mixer Minimal Game client available [here](https://github.com/mixer/interactive-node-samples/tree/master/minimal-game-client).

# Custom Controls
Under src there are 4 main areas of interest: scripts.js, index.html, resources, and styles.css. scripts.js defines the buttons of the gamepad we are interested in, sets up a simple loop to poll, and has a bit of logic to understand what buttons or sticks are being used and how to visualize them. index.html includes the CDK-STD and JQuery as well as putting all the images that we'll be showing and hiding in the document. resources contains all the images. They are authored as naive overlays that are meant to all have the same origin. Lastly, styles.css defines two classes so we can easily show and hide overlay images.