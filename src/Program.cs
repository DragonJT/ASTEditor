
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.Serialization;
using Raylib_cs;

static class RaylibHelper
{
    public static bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key);
    }
}

interface IDisplay
{
    void Display(DisplayNodeTree displayNodeTree, Node node);
}

class DisplayParameters(Color punctuationColor) : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText("(", punctuationColor);
        for (var i = 0; i < node.children.Count; i++)
        {
            displayNodeTree.Draw(node.children[i]);
            if (i < node.children.Count - 1)
            {
                displayNodeTree.DrawText(",", punctuationColor);
                displayNodeTree.DrawSpace();
            }
        }
        displayNodeTree.DrawText(")", punctuationColor);
    }
}

class DisplayChildX(int x) : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.Draw(node.children[x]);
    }
}

class DisplayValue(Color color) : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.value!, color);
    }
}

class DisplayNewLine : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.NewLine();
    }
}

class DisplaySpace : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawSpace();
    }
}

class DisplayType(Color color) : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.nodeType.name, color);
    }
}

class DisplayChildren : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawChildren(node);
    }
}

class DisplayChildrenOfChild(int childID) : IDisplay
{
    public void Display(DisplayNodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawChildren(node.children[childID]);
    }
}

class DefaultTypes
{
    public NodeType[] types;
    public int id;

    public DefaultTypes(NodeType[] types, int id)
    {
        this.types = types;
        this.id = id;
    }
}

class NodeType
{
    public string name;
    public bool hasValue;
    public DefaultTypes? defaultTypes;
    public List<NodeType> subTypes = [];
    public List<IDisplay> display = [];

    public NodeType(string name, bool hasValue)
    {
        this.name = name;
        this.hasValue = hasValue;
    }
}

class Node
{
    public NodeType nodeType;
    public string? value;
    public Node? parent = null;
    public List<Node> children = [];

    public Node(Node? parent, NodeType nodeType, string? value)
    {
        this.parent = parent;
        this.nodeType = nodeType;
        this.value = value;
    }
}

class MenuItem
{
    public string name;
    public Action action;

    public MenuItem(string name, Action action)
    {
        this.name = name;
        this.action = action;
    }
}

class DisplayNodeTree
{
    int x;
    int y;
    int depth;
    int fontSize = 50;
    int spaceSize;

    public void Reset()
    {
        x = 0;
        y = 0;
        depth = 0;
        spaceSize = Raylib.MeasureText(" ", fontSize);
    }

    public void DrawText(string text, Color color)
    {
        Raylib.DrawText(text, x+2, y, fontSize, color);
        x += Raylib.MeasureText(text, fontSize)+4;
    }

    public void DrawSpace()
    {
        x += spaceSize;
    }

    public void NewLine()
    {
        y += fontSize;
        x = 0;
    }

    public void DrawChildren(Node node)
    {
        depth++;
        foreach (var c in node.children)
        {
            x += spaceSize * 4 * depth;
            Draw(c);
        }

        depth--;
    }

    static Action CreateForm(Vector2 pos, int fontSize, Node node, NodeType nodeType)
    {
        return () =>
        {
            var textBox = new TextBox(new Rectangle(pos.X, pos.Y, 600, fontSize), fontSize);
            textBox.onEnter = () =>
            {
                var newNode = new Node(node, nodeType, textBox.text);
                if (nodeType.defaultTypes != null)
                {
                    foreach(var n in nodeType.defaultTypes.types)
                    {
                        newNode.children.Add(new Node(newNode, n, "void"));
                    }
                }
                node.children.Add(newNode);
                Program.gui = null;
            };
            Program.gui = textBox;
        };
    }

    static void CreateAddMenuItems(Menu menu, Vector2 pos, int fontSize, Node node)
    {
        var addNodeMenuItem = node.nodeType.subTypes
            .Select(t => new MenuItem($"Add {t.name}", CreateForm(pos, fontSize, node, t)))
            .ToArray();
        menu.menuItems.AddRange(addNodeMenuItem);
        
    }

    public void Draw(Node node)
    {
        var startY = y;
        foreach (var display in node.nodeType.display)
        {
            display.Display(this, node);
        }
        var m = Raylib.GetMousePosition();
        if (m.Y > startY && m.Y < startY + fontSize && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            var menu = new Menu(m, 400, fontSize);
            if (node.nodeType.defaultTypes == null)
            {
                CreateAddMenuItems(menu, m, fontSize, node);
            }
            else if (node.nodeType.defaultTypes.id >= 0)
            {
                CreateAddMenuItems(menu, m, fontSize, node.children[node.nodeType.defaultTypes.id]);
            }
            if (node.nodeType.hasValue)
        {
            menu.menuItems.Add(new MenuItem("Rename", () =>
            {
                var textBox = new TextBox(new Rectangle(m.X, m.Y, 600, fontSize), fontSize);
                textBox.onEnter = () =>
                {
                    node.value = textBox.text;
                    Program.gui = null;
                };
                Program.gui = textBox;
            }));
        }
        if (node.parent != null)
        {
            menu.menuItems.Add(new MenuItem("Delete", () =>
            {
                node.parent.children.Remove(node);
                Program.gui = null;
            }));
        }
        Program.gui = menu;
        }
    }
}

class Program
{
    public static IGUI? gui;

    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "IDE");
        Raylib.MaximizeWindow();

        var displayNodeTree = new DisplayNodeTree();

        var exprType = new NodeType("expression", true);
        var typeType = new NodeType("type", true);
        var nameType = new NodeType("name", true);
        var paramsType = new NodeType("parameters", false);
        var bodyType = new NodeType("statements", false);
        var funcType = new NodeType("function", true);
        var classType = new NodeType("class", true);
        var moduleType = new NodeType("module", true);
        var rootType = new NodeType("root", false);

        rootType.subTypes.Add(moduleType);
        moduleType.subTypes.AddRange([classType, funcType]);
        classType.subTypes.AddRange([funcType]);
        bodyType.subTypes.AddRange([exprType]);

        funcType.defaultTypes = new DefaultTypes([typeType, paramsType, bodyType], 2);
        paramsType.defaultTypes = new DefaultTypes([typeType, nameType], -1);
        typeType.display.Add(new DisplayValue(Color.Blue));

        paramsType.display.Add(new DisplayParameters(Color.DarkGray));

        rootType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        moduleType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayNewLine(),
            new DisplayChildren()]);

        classType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.Blue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        funcType.display.AddRange([
            new DisplayChildX(0),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayChildX(1),
            new DisplayNewLine(),
            new DisplayChildrenOfChild(2),
        ]);
        var root = new Node(null, rootType, null);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            displayNodeTree.Reset();
            displayNodeTree.Draw(root);
            if (gui != null)
            {
                gui.Update();
            }
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}