# Unity2D Pixel Perfect Collider
Unity2D Pixel Perfect Collider is a simple C# script that when added to a 2D GameObject in unity will create a pixel perfect polygon collider based off of that GameObject's sprite.

Development has temporarily stopped on this project as I focus on other things. Feel free to keep using this script but just know it might not continue to work in newer versions of unity are released.

Note: You need to tick the "read/write enabled" tickbox for all sprites you want this to work on!

# Edits for PowerQuest by Dave Lloyd
- Caching components so they're not updated every frame
- Getting PowerQuest specific sprite component to set the sprite offset same as the collider offset
- Checking if sprite has changed before regenerating
- When sprite is null, collider is no longer reset, it just stays the same