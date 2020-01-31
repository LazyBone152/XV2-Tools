# MonoGame Wpf Interop

This adds Wpf support to MonoGame.

You can host as many MonoGame controls in Wpf as you want. Note that WPF is limited to 60 FPS.

# Important changes

1. Derive from MonoGame.Framework.WpfInterop.WpfGame instead of Microsoft.Xna.Framework.Game
2. Keyboard and Mouse events from MonoGame classes will not work with this implementation. Use WpfKeyboard and WpfMouse instead. Read this issue for details: https://github.com/MarcStan/MonoGame.Framework.WpfInterop/issues/1
3. GraphicsDeviceManager can no longer be used (it requires a reference to Game). Use WpfGraphicsDeviceService instead (it requires a reference to WpfGame).
4. Using rendertargets works, but requires slightly different code (namely there is no backbuffer when you set a "null" rendertarget).
    You need to grab the existing (internal) rendertarget before setting your own and afterwards set this rendertarget again. See the section "RenderTargets" in the readme: https://github.com/MarcStan/MonoGame.Framework.WpfInterop

## Example

public class MyGame : WpfGame
{
    private IGraphicsDeviceService _graphicsDeviceManager;
    private WpfKeyboard _keyboard;
    private WpfMouse _mouse;

    protected override void Initialize()
    {
        // must be initialized. required by Content loading and rendering (will add itself to the Services)
        // note that MonoGame requires this to be initialized in the constructor, while WpfInterop requires it to
        // be called inside Initialize (before base.Initialize())
        _graphicsDeviceManager = new WpfGraphicsDeviceService(this);

        // wpf and keyboard need reference to the host control in order to receive input
        // this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
        _keyboard = new WpfKeyboard(this);
        _mouse = new WpfMouse(this);
        
        // must be called after the WpfGraphicsDeviceService instance was created
        base.Initialize();

        // content loading now possible
    }

    protected override void Update(GameTime time)
    {
        // every update we can now query the keyboard & mouse for our WpfGame
        var mouseState = _mouse.GetState();
        var keyboardState = _keyboard.GetState();
    }

    protected override void Draw(GameTime time)
    {
    }
}


Now you can use this class in any of your WPF application:

<MyGame Width="800" Height="480" />

Find more details in the readme at: https://github.com/MarcStan/MonoGame.Framework.WpfInterop

# Special note with when hosting WpfGame inside TabControls

See section TabControls in the main readme at: https://github.com/MarcStan/MonoGame.Framework.WpfInterop
