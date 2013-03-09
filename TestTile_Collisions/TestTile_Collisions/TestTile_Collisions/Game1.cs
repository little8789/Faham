using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;
using System.Diagnostics;

namespace TestTile_Collisions
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Player player;
        
        public int screenWidth, screenHeight;
        public int layerWidth, layerHeight;
        public int numberOfRowTilesInMap = 33;
        public int numberOfColTilesInMap = 24;
        public int tileSize = 64;

        //xTile stuff
        Map map;
        IDisplayDevice mapDisplayDevice;
        xTile.Dimensions.Rectangle viewport;  //xTiles viewport
        public Layer mainLayer;               //main layer in the map (there is only one layer in this map
        xTile.Tiles.TileArray tileArray;      //array of tiles in the mainLayer
        int buildMode;                        //int value used for debugging (ex. log("Something", 1) will allow priority to be greater than 0 which will allow it to print
        


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            buildMode = 1;
        }
              

        protected override void Initialize()
        {
            base.Initialize();

            //screen height and width of xna's viewport
            screenWidth = GraphicsDevice.Viewport.Width;
            screenHeight = GraphicsDevice.Viewport.Height;

            //Used for finding player position in a tile in map
            this.layerWidth = numberOfRowTilesInMap * tileSize;
            this.layerHeight = numberOfColTilesInMap * tileSize;

            mapDisplayDevice = new XnaDisplayDevice(Content, GraphicsDevice);
            map.LoadTileSheets(mapDisplayDevice);
            viewport = new xTile.Dimensions.Rectangle(new Size(screenWidth, screenHeight));

            Vector2 playerStartPosition = new Vector2(GraphicsDevice.Viewport.Width/2, GraphicsDevice.Viewport.Height/2 + 50);
            
            //load the layer
            mainLayer = map.Layers[0];
            //load the tiles into an array
            tileArray = mainLayer.Tiles;            

            //log("l: " + numberOfRowTilesInMap + " h: " + numberOfColTilesInMap + "tile: " + tileSize);
            //log("Layer width: " + layerWidth + ".. Layer height: " + layerHeight);

            this.player = new Player(this);
            this.player.Initialize(playerStartPosition, 0.0f, this, numberOfRowTilesInMap, numberOfColTilesInMap, tileSize);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
           
            //load the map
            map = Content.Load<Map>("Maps\\TestMap_passable");
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) this.Exit();
            KeyboardState ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.Escape)) this.Exit();

            //Update player
            this.player.Update(gameTime);

            //Update xTile map 
            map.Update(gameTime.ElapsedGameTime.Milliseconds);
            //move the map viewport relative to playerPostion
            viewport.X = (int)player.playerPosition.X;
            viewport.Y = (int)player.playerPosition.Y;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            //Draw the player
            this.player.Draw(spriteBatch);

            //render xTile map
            map.Draw(mapDisplayDevice, viewport);

            spriteBatch.End();
            base.Draw(gameTime);
        }             

        //Method used for debugging
        protected void log(string msg, int priority = 0)
        {
            if (priority >= buildMode)
                Console.WriteLine(msg);
        }
    }
}
