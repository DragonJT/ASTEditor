
using Raylib_cs;

static class RaylibHelper
{
    public static bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key);
    }

    public static Rectangle Expand(this Rectangle rect, float radius)
    {
        return new Rectangle(rect.X - radius, rect.Y - radius, rect.Width + radius * 2, rect.Height + radius * 2);
    }

    public static string GetText()
    {
        var text = "";
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125))
                text += (char)key;

            key = Raylib.GetCharPressed();
        }
        return text;
    }
}

abstract class Node
{
    public string value = "";
    public Node? parent = null;
    public List<Node> children = [];

    public Node(Node? parent)
    {
        this.parent = parent;
    }

    public abstract void Draw(Window window, NodeTree nodeTree);
    public abstract void Input(Window window, NodeTree nodeTree, string text);

    public Node[] DescendingNodes()
    {
        return [this, .. children.SelectMany(c => c.DescendingNodes())];
    }

    public Node Root => parent != null ? parent.Root : this;
}

class NodeTree : GUI
{
    Node root;
    public Node selected;
    int x;
    int y;
    int depth;

    public NodeTree(Node root) : base(true)
    {
        this.root = root;
        selected = root;
    }

    public void DrawText(string text, Color color)
    {
        Raylib.DrawText(text, x + 2, y, Program.style.fontSize, color);
        x += Raylib.MeasureText(text, Program.style.fontSize) + 4;
    }

    public void DrawSpace()
    {
        x += Program.style.spaceSize;
    }

    public void NewLine()
    {
        y += Program.style.fontSize;
        x = 0;
    }

    public void DrawChildren(Window window, List<Node> children)
    {
        depth++;
        foreach (var c in children)
        {
            x = Program.style.spaceSize * depth * 4 + Program.style.border;
            c.Draw(window, this);
        }
        depth--;
    }

    public void DrawSelected(Window window, Node node)
    {
        if (node == selected)
        {
            Raylib.DrawRectangle((int)window.rect.X, y, (int)window.rect.Width, Program.style.fontSize, Color.RayWhite);
            Raylib.DrawRectangleLines((int)window.rect.X, y, (int)window.rect.Width, Program.style.fontSize, Color.Black);
        }
    }

    public override int Height(Window window)
    {
        return (int)(window.rect.Height - window.y - Program.style.border);
    }

    void MoveSelected(int delta)
    {
        var nodes = root.DescendingNodes();
        if(nodes.Length == 0) return;
        var index = Array.IndexOf(nodes, selected);
        index+=delta;
        if (index < 0) index = nodes.Length - 1;
        else if (index >= nodes.Length) index = 0;
        selected = nodes[index];
    }

    public override void Update(Window window)
    {
        var active = window.IsActive(this);
        y = window.y;
        x = (int)(window.rect.X + Program.style.border);
        if (active)
        {
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
            {
                MoveSelected(-1);
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
            {
                MoveSelected(1);
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
            {
                selected.Input(window, this, "");
            }
            var text = RaylibHelper.GetText();
            if (text.Length > 0)
            {
                selected.Input(window, this, text);
            }
        }

        root.Draw(window, this);
    }
}

interface IType
{
    string Name{ get; }
}

class Primitive(string name, Type type) : IType
{
    public string Name{ get; } = name;
    public Type type = type;
}

class Program
{
    public static Style style = new();
    public static List<Primitive> primitives = [];

    static void Main()
    {
        primitives.Add(new Primitive("int", typeof(int)));
        primitives.Add(new Primitive("float", typeof(float)));
        primitives.Add(new Primitive("void", typeof(void)));
        primitives.Add(new Primitive("double", typeof(double)));
        primitives.Add(new Primitive("long", typeof(long)));
        primitives.Add(new Primitive("string", typeof(string)));
        primitives.Add(new Primitive("char", typeof(char)));

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "ASTEditor");
        Raylib.MaximizeWindow();
        Raylib.SetExitKey(KeyboardKey.Null);

        var root = new Root();

        var form = new Window(null, new Rectangle(0, 0, 1000, 800));
        form.Add(new NodeTree(root));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            form.Update();

            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}