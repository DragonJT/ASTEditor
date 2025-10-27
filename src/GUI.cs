using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

abstract class GUIWindow
{
    public bool startFrame = true;
    public GUIWindow? parent;
    public GUIWindow? child;
    public abstract void Update();

    public void SetParent(GUIWindow? parent)
    {
        if(parent != null)
        {
            this.parent = parent;
            parent.child = this;
        }
    }

    public void Delete()
    {
        parent!.child = null;
    }

    public bool Active => child == null && !startFrame;
}

abstract class GUI(GUIWindow guiWindow)
{
    public readonly GUIWindow guiWindow = guiWindow;
    public abstract void Update();
    public bool Active => guiWindow.Active;
}

class Menu : GUIWindow
{
    Vector2 position;
    float width;
    int fontSize;
    public List<MenuItem> menuItems = [];
    int index = 0;

    public Menu(GUIWindow? parent, Vector2 position, float width, int fontSize)
    {
        SetParent(parent);
        this.position = position;
        this.width = width;
        this.fontSize = fontSize;
    }

    public override void Update()
    {
        float height = fontSize * menuItems.Count;
        var rect = new Rectangle(position.X, position.Y, width, height);
        if (Active)
        {
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
            {
                index--;
                if (index < 0) index = menuItems.Count - 1;
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
            {
                index++;
                if (index >= menuItems.Count) index = 0;
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
            {
                menuItems[index].action();
            }
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        for (var i = 0; i < menuItems.Count; i++)
        {
            var r = new Rectangle(rect.X, rect.Y + i * fontSize, rect.Width, fontSize);
            if (i == index)
            {
                Raylib.DrawRectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, Color.Red);
            }
            Raylib.DrawText(menuItems[i].name, (int)rect.X, (int)(rect.Y + i * fontSize), fontSize, Color.Black);
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        startFrame = false;
    }
}

class TextBox : GUI
{
    int fontSize;
    Rectangle rect;
    public string text = "";
    public Action? onEnter;

    public TextBox(GUIWindow guiWindow, Rectangle rect, int fontSize) : base(guiWindow)
    {
        this.rect = rect;
        this.fontSize = fontSize;
    }

    public override void Update()
    {
        if (Active)
        {
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if ((key >= 32) && (key <= 125))
                    text += (char)key;

                key = Raylib.GetCharPressed();
            }

            if (RaylibHelper.IsKeyPressed(KeyboardKey.Backspace) && text.Length > 0)
            {
                text = text[..^1];
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
            {
                onEnter?.Invoke();
            }
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, fontSize, Color.Black);
    }
}

class Label : GUI
{
    Vector2 position;
    string label;
    int fontSize;
    Color color;

    public Label(GUIWindow guiWindow, Vector2 position, string label, int fontSize, Color color) : base(guiWindow)
    {
        this.position = position;
        this.label = label;
        this.fontSize = fontSize;
        this.color = color;
    }

    public override void Update()
    {
        Raylib.DrawText(label, (int)position.X, (int)position.Y, fontSize, color);
    }
}

class Form : GUIWindow
{
    Rectangle rect;
    List<GUI> guis = [];
    float y;
    int fontSize;
    const int spacing = 5;
    const int border = 20;

    public Form(GUIWindow? parent, Rectangle rect, int fontSize)
    {
        SetParent(parent);
        this.rect = rect;
        y = rect.Y + border;
        this.fontSize = fontSize;
    }

    public void AddHeader(string text, float fractionFontSize)
    {
        var headerFontSize = (int)(fontSize * fractionFontSize);
        var length = Raylib.MeasureText(text, headerFontSize);
        guis.Add(new Label(this, new Vector2(rect.Width / 2 + rect.X - length / 2f, y), text, headerFontSize, Color.DarkGray));
        y += headerFontSize + spacing;
    }

    public void AddLabel(string text)
    {
        guis.Add(new Label(this, new Vector2(rect.X + border, y), text, fontSize, Color.Black));
    }

    public TextBox AddTextBox()
    {
        var textBox = new TextBox(this, new Rectangle(rect.X + rect.Width * 0.3f, y, rect.Width * 0.7f - border, fontSize), fontSize);
        y += fontSize + spacing;
        guis.Add(textBox);
        return textBox;
    }

    public void AddNodeTree(Node root)
    {
        guis.Add(new NodeTree(this, root, fontSize));
    }

    public override void Update()
    {
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        foreach (var g in guis)
        {
            g.Update();
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        child?.Update();
        startFrame = false;
    }
}