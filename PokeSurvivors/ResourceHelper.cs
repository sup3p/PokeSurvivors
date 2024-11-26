using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using System.Reflection;
using Il2CppVampireSurvivors.Graphics;
using Il2CppSystem.Collections.Generic;

namespace PokeSurvivors
{
    public static class ResourceHelper
    {
        public static Texture2D LoadTexture(string resourceName)
        {

            // Get the assembly where the resource is embedded
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Format the full resource name: typically "Namespace.FolderName.FileName"
            string resourcePath = $"{assembly.GetName().Name}.{resourceName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    Core.melonLog.Msg($"Resource {resourcePath} not found.");
                    Debug.LogError($"Resource {resourcePath} not found.");
                    return null;
                }

                // Read the image data from the stream
                byte[] imageData = new byte[stream.Length];
                stream.Read(imageData, 0, imageData.Length);

                // Load image data into a Texture2D
                Texture2D texture = new Texture2D(2, 2);

                if (!ImageConversion.LoadImage(texture, imageData))
                {
                    throw new Exception("ImageConversion.LoadImage failed");
                }

                texture.filterMode = FilterMode.Point;
                texture.name = resourceName;

                return texture;
            }
        }

        public static Sprite LoadSprite(Texture2D texture, Rect rect, Vector2 pivot)
        {
            Sprite sprite = Sprite.Create(texture, rect, pivot);
            sprite.name = texture.name;
            return sprite;
        }

        public static Texture2D CreateCharacterTexture(string assetName, string textureName)
        {
            Texture2D createdTexture = LoadTexture(assetName);
            createdTexture.name = textureName;
            Core.characterTextures.Add(textureName, createdTexture);
            return createdTexture;
        }

        public static Sprite CreateCharacterSprite(Texture2D characterTexture, int x, int y, int width, int height, string spriteName, Vector2 pivot)
        {
            Sprite newSprite = LoadSprite(characterTexture, new Rect(x, y, width, height), new Vector2(0.5f, 0));
            newSprite.name = spriteName;
            SpriteManager.RegisterSprite(newSprite);
            return newSprite;
        }

        public static Il2CppSystem.Collections.Generic.List<Sprite> CreateCharacterSpritesFromStrip(Texture2D characterTexture, int x, int y, int width, int height, Vector2 pivot, string spriteName, int frames)
        {
            //Returns a list of sprite objects from a texture sheet where all frames are adjacent and have the same size
            Il2CppSystem.Collections.Generic.List<Sprite> sprites = new();

            for (int i = 0; i < frames; i++)
            {
                string newSpriteName = spriteName + "_" + (i + 1).ToString("00");
                if (spriteName == "wevs_babi-onna")
                {
                    //Core.melonLog.Msg("Onna override");
                    newSpriteName = $"onna_{1 + i:00}";
                }
                Sprite newSprite = CreateCharacterSprite(characterTexture, x + (width * i), y, width, height, newSpriteName, pivot);
                /*
                if (spriteName == "wevs_onna") {
                    newSprite.name = $"onna_${(1 + i).ToString("00")}";
                    Core.melonLog.Msg("Onna override");
                }
                */
                sprites.Add(newSprite);
            }

            Core.animationSprites.Add(spriteName, sprites);
            return sprites;
        }
    }
}
