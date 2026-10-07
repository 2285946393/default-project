---
title: Adding Nodes to Scenes
parent: Scenes
---

`AddedNode` is a version of [`SpireField`]({% link docs/utilities/spirefield.md %}) for easily adding a new node to an existing scene.

It is defined similarly to a `SpireField`, receiving a TargetType and a FieldType, but both these types must have `Node` as a base type.

`public static readonly AddedNode<TargetNode, NodeType> NewNodeInScene = new((targetNode)=>default value);`

The default value will be added to the target node when its `_Ready` method is called. You can also add to the node yourself in the default value function if you want to ensure it is set up exactly how you want. The returned value will not be added again if it is already a child.

The resulting node can be retrieved as with any other `SpireField` given an instance of the modified node. The value should not be set after the initial generation.

You can also provide the filepath to a packed scene (.tscn) resource instead of a function, which will be instantiated and added as a child. If you want to adjust how the scene is added as a child, you may pass an additional function which will receive the parent and instantiated scene before it is added as a child.

```cs
// A basic example of adding an image+label to all cards.
// For actual implementation you would be recommended to use a predefined scene instead.
public partial class CardExtraCostDisplay : Control
{
    public static AddedNode<NCard, CardExtraCostDisplay> Node = new((card) =>
    {
        //Would suggest loading a scene rather than this manual node setup
        var control = new CardExtraCostDisplay();
        
        var tex = ResourceLoader.Load<Texture2D>("res://Mod/images/image.png");
        
        var size = tex.GetSize();
        var texRect = new TextureRect();
        texRect.Name = tex.ResourcePath;
        texRect.Size = new(50, 50);
        texRect.Texture = tex;
        texRect.PivotOffset = size / 2f;
        texRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        texRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        texRect.MouseFilter = MouseFilterEnum.Ignore;
        
        control.Size = new(50, 50);
        control.Position = new(-126, -231);
        control.AddChild(texRect);
        
        var label = new Label { Text = "1" };
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        control.AddChild(label);

        //For cards specifically, this is necessary to use the CardContainer instead of the NCard parent node, 
        //which does not receive all the transforms.
        var cardContainer = card.GetChild(0)!;
        cardContainer.AddChild(control);

        //Changing position to before the star icon node.
        cardContainer.MoveChild(control, cardContainer.GetNode("%StarIcon").GetIndex());

        return control;
    });
}
```

Similar example using a scene path:
```cs
public class AddedNodes
{
    public static AddedNode<NCard, CardExtraCostDisplay> NodeFromSceneFile = new(
        "res://Mod/scenes/ExtraCostDisplay.tscn",
        (card, display) =>
        {
            //If additional setup is required, that can also be done here.
            
            var cardContainer = card.GetChild(0)!;
            cardContainer.AddChild(display);

            //Changing position to before the star icon node.
            cardContainer.MoveChild(display, cardContainer.GetNode("%StarIcon").GetIndex());
        }
    );
}
```