
using System.Numerics;
using System.Reflection;
using Raylib_cs;

class Window
{
    public int selectedID = 0;
    public bool startFrame = true;
    public Window? parent;
    public Window? child;
    Rectangle rect;
    List<GUI> guis = [];
    List<GUI> selectableGUIs = [];
    float y;
    int fontSize;
    const int spacing = 5;
    const int border = 20;
    const float labelFraction = 0.4f;

    public float Y => y;

    public Window(Window? parent, Rectangle rect, int fontSize)
    {
        SetParent(parent);
        this.rect = rect;
        y = rect.Y + border;
        this.fontSize = fontSize;
    }

    void Add(GUI gui)
    {
        guis.Add(gui);
        if (gui.isSelectable)
        {
            selectableGUIs.Add(gui);
        }
    }

    public void AddHeader(string text, float fractionFontSize)
    {
        var headerFontSize = (int)(fontSize * fractionFontSize);
        var length = Raylib.MeasureText(text, headerFontSize);
        var r = new Rectangle(rect.Width / 2 + rect.X - length / 2f, y, length, headerFontSize);
        Add(new Label(this, r, text, headerFontSize, Color.DarkGray));
        y += headerFontSize + spacing;
    }

    public void AddLabel(string text)
    {
        Add(new Label(this, new Rectangle(rect.X + border, y, rect.Width * 0.3f, fontSize), text, fontSize, Color.Black));
    }

    public TextBox AddTextBox(ValueGetSetter valueGetSetter)
    {
        var r = new Rectangle(rect.X + rect.Width * labelFraction, y, rect.Width * (1 - labelFraction) - border, fontSize);
        var textBox = new TextBox(this, r, fontSize, valueGetSetter);
        y += fontSize + spacing;
        Add(textBox);
        return textBox;
    }

    public void AddButton(string text, Action action)
    {
        Add(new Button(this, new Rectangle(rect.X + border, y, rect.Width - border * 2, fontSize), text, fontSize, action));
        y += fontSize;
        if(rect.Y + rect.Height < y)
        {
            rect.Height = y - rect.Y + border;
        }
    }

    public void AddBoolBox(bool value)
    {
        Add(new BoolBox(this, new Rectangle(rect.X + rect.Width * labelFraction, y, fontSize, fontSize), value));
        y += fontSize;
    }

    public void AddNodeTree(Node root)
    {
        Add(new NodeTree(this, new Rectangle(rect.X + border, y, rect.Width - border * 2, rect.Height - y - border), root, fontSize));
    }

    public void Update()
    {
        if (Active && selectableGUIs.Count > 0)
        {
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
            {
                selectedID--;
                if (selectedID < 0) selectedID = selectableGUIs.Count - 1;
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
            {
                selectedID++;
                if (selectedID > selectableGUIs.Count - 1) selectedID = 0;
            }
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        foreach (var g in guis)
        {
            g.Update();
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        child?.Update();
        startFrame = false;
    }

    public void SetParent(Window? parent)
    {
        if (parent != null)
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

    public bool IsActive(GUI gui)
    {
        if (Active)
        {
            var index = selectableGUIs.IndexOf(gui);
            return index == selectedID;
        }
        return false;
    }
}

abstract class GUI(Window window, Rectangle rect, bool isSelectable)
{
    public bool isSelectable = isSelectable;
    public readonly Window window = window;
    public Rectangle rect= rect;

    public abstract void Update();
    public bool Active => window.IsActive(this);
}

class TextBox : GUI
{
    int fontSize;
    ValueGetSetter valueGetSetter;
    public string text = "";
    public Action? onEnter;

    public TextBox(Window window, Rectangle rect, int fontSize, ValueGetSetter valueGetSetter) : base(window, rect, true)
    {
        this.fontSize = fontSize;
        this.valueGetSetter = valueGetSetter;
    }

    public override void Update()
    {
        text = valueGetSetter.GetString();
        Color border = Color.Black;
        if (Active)
        {
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if ((key >= 32) && (key <= 125))
                {
                    text += (char)key;
                    valueGetSetter.SetString(text);
                }

                key = Raylib.GetCharPressed();
            }

            if (RaylibHelper.IsKeyPressed(KeyboardKey.Backspace) && text.Length > 0)
            {
                text = text[..^1];
                valueGetSetter.SetString(text);
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
            {
                onEnter?.Invoke();
            }
            border = Color.Red;
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(rect, 2, border);
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, fontSize, Color.Black);
    }
}

class Label : GUI
{
    string label;
    int fontSize;
    Color color;

    public Label(Window window, Rectangle rect, string label, int fontSize, Color color) : base(window, rect, false)
    {
        this.label = label;
        this.fontSize = fontSize;
        this.color = color;
    }

    public override void Update()
    {
        Raylib.DrawText(label, (int)rect.X, (int)rect.Y, fontSize, color);
    }
}

class Button : GUI
{
    string text;
    int fontSize;
    Action action;

    public Button(Window window, Rectangle rect, string text, int fontSize, Action action) : base(window, rect, true)
    {
        this.text = text;
        this.fontSize = fontSize;
        this.action = action;
    }

    public override void Update()
    {
        if (Active)
        {
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.Red);
        }
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, fontSize, Color.Black);
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter) && Active)
        {
            action();
        }
    }
}

class BoolBox : GUI
{
    bool value;

    public BoolBox(Window window, Rectangle rect, bool value) : base(window, rect, true)
    {
        this.value = value;
    }

    public override void Update()
    {
        Color color = Color.Black;
        if (Active)
            color = Color.Red;

        if (value)
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, color);
        else
            Raylib.DrawRectangleLinesEx(rect, 4, color);

        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter) && Active)
            value = !value;
    }
}