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

using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;
using System.Diagnostics;

namespace TestTile_Collisions
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Player : Microsoft.Xna.Framework.GameComponent
    {
        Game1 game;
        Location tileLocation;

        public Vector2 playerPosition;
        public Vector2 velocity;
        public float rotation;

        public Texture2D playerTexture;
        public Microsoft.Xna.Framework.Rectangle playerDestination;
        public Microsoft.Xna.Framework.Rectangle source;             //determines which sprite to use in the sprite sheet

        int frameCount = 0;    // Which frame we are.  Values = {0, 1, 2}
        int frameSkipX = 64;   // How much to move the frame in X when we increment a frame--X distance between top left corners.
        int frameStartX = 0;   // X of top left corner of frame 0. 
        int frameStartY = 0;   // Y of top left corner of frame 0.
        int frameWidth = 64;   // X of right 
        int frameHeight = 64;  // Y of bottom

        int animationCount; // How many ticks since the last frame change.
        int animationMax = 10; // How many ticks to change frame after.

        float acceleration = 0.05f;
        float rotationAcceleration = 0.02f * (float)Math.PI;
        float MAX_SPEED = 2.0f;
        float friction = 0.03f;

        int buildMode = 1;

        int layerWidth;
        int layerHeight;
        int tileSize;


        public Player(Game game)
            : base(game)
        {
        }

        public virtual void Initialize(Vector2 startPosition, float newRotation, Game1 newGame, int lw, int lh, int ts)
        {
            game = newGame;
            this.layerWidth = lw;
            this.layerHeight = lh;
            this.tileSize = ts;


            this.playerPosition = startPosition;
            this.rotation = newRotation;

            this.frameCount = 2; //start with middle sprite (feet are together)

            this.playerDestination = new Microsoft.Xna.Framework.Rectangle((int)playerPosition.X - frameWidth / 2,
                (int)playerPosition.Y - frameHeight / 2, this.frameWidth, this.frameHeight);

            this.source = new Microsoft.Xna.Framework.Rectangle(frameStartX + frameSkipX * this.frameCount, this.frameStartY,
                this.frameWidth, this.frameHeight);

            this.animationCount = 0;

            this.playerTexture = Game.Content.Load<Texture2D>("guard_spriteSheet");


            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // Moving update:
            KeyboardState ks = Keyboard.GetState();

            Vector2 oldPos = playerPosition;            

            // Move faster or slower.
            if (ks.IsKeyDown(Keys.Up))
            {
                if (this.velocity.X < MAX_SPEED && this.velocity.Y < MAX_SPEED)
                {
                    this.velocity.Y += (float)(this.acceleration * Math.Cos(this.rotation + Math.PI));
                    this.velocity.X += (float)(this.acceleration * Math.Sin(this.rotation));
                }

                this.animationCount += 1;
            }
            else if (ks.IsKeyDown(Keys.Down))
            {
                if (this.velocity.X < MAX_SPEED && this.velocity.Y < MAX_SPEED)
                {
                    this.velocity.Y -= (float)(this.acceleration * Math.Cos(this.rotation + Math.PI));
                    this.velocity.X -= (float)(this.acceleration * Math.Sin(this.rotation));
                }

                this.animationCount += 1;
            }

            //// Turn. 
            if (ks.IsKeyDown(Keys.Right))
            {
                this.rotation += this.rotationAcceleration;
            }
            if (ks.IsKeyDown(Keys.Left))
            {
                this.rotation -= this.rotationAcceleration;
            }

            //Ensure player never leaves the map
            if (playerPosition.X < playerDestination.Width / 2)
                playerPosition.X = playerDestination.Width / 2;
            if (playerPosition.Y < playerDestination.Height / 2)
                playerPosition.Y = playerDestination.Height / 2;

            if (playerPosition.X + playerDestination.Width > 33*64)
                playerPosition.X = 33 * 64 - playerDestination.Width;
            if (playerPosition.Y + playerDestination.Height > 24*64)
                playerPosition.Y = 24 * 64 - playerDestination.Height;


            // Include friction.
            // If our velocity (scalar magnitude of a vector = length of a vector) is greater than the effect of friction,
            // then friction should be applied in the opposite direction of the velocity.  
            if (Math.Abs(this.velocity.Length()) > this.friction)
            {
                this.velocity.X -= Math.Sign(this.velocity.X) * this.friction; // Whatever sign velocity is, 
                this.velocity.Y -= Math.Sign(this.velocity.Y) * this.friction; // apply friction in the opposite direction.
            }
            else
            { // If our velocity is closer to zero than the effect of friction, we should just stop. 
                this.velocity.X = 0;
                this.velocity.Y = 0;
            }


            //Apply the velocity to the position
            this.playerPosition.X += this.velocity.X;
            this.playerPosition.Y += this.velocity.Y;


            //Get the players tile location based on his position 
            tileLocation = new Location((int)playerPosition.X / 32, (int)playerPosition.Y / 32);   //not sure why 32 yet, but it allowed for correct tileLocation not 64
            log("New Location: " + tileLocation.ToString(),1);

            //Check for COLLISIONS 
            //with any tiles that have the property "passable" = False
            if (Collision(tileLocation))
            {
                log("Collision", 1);
                playerPosition = oldPos;
            }

            //Do the animation
            this.UpdateAnimation();

            base.Update(gameTime);
        }

       

        public void Draw(SpriteBatch spriteBatch)
        {
            //spriteBatch.Begin();

            //Update destination rectangle 
            playerDestination.X = (int)playerPosition.X - (int)(playerDestination.Width / 2);
            playerDestination.Y = (int)playerPosition.Y - (int)(playerDestination.Height / 2);

            // Update the source rectangle, based on where in the animation we are.  
            this.source.X = this.frameStartX + this.frameSkipX * this.frameCount;

            Vector2 rotationOrigin = new Vector2(this.source.Width / 2, this.source.Height / 2); 

            //Draw the player
            spriteBatch.Draw(this.playerTexture, this.playerDestination, this.source, Color.White, this.rotation, rotationOrigin, SpriteEffects.None, 0);

            //spriteBatch.End();
        }

        /**
        * Check for tile property that the playerPosition is hitting
        * Return true if it is passable (floor tile)
        * false if it is not passable (wall tile)
        * Note: Doesn't handle null tile situation
        * */
        public bool Collision(Location l)
        {
            bool passable = game.mainLayer.Tiles[l].Properties["passable"];     //passable=false -> means it is a wall tile
            return !passable;
        }

        //Method used for error checking
        protected void log(string msg, int priority = 0)
        {
            if (priority >= buildMode)
                Console.WriteLine(msg);
        }

        private void UpdateAnimation()
        {
            if (this.animationCount > this.animationMax)
            {
                this.animationCount = 0;
                this.frameCount += 1;
            }

            if (this.frameCount == 3)
            {
                this.frameCount = 0;
            }                 
        }
    }
}
