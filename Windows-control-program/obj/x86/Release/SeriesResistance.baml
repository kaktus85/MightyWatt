   M S B A M L   `   `   `  ���� E  AMightyWatt, Version=1.3.1.0, Culture=neutral, PublicKeyToken=null'clr-namespace:MightyWatt
MightyWatt  !    MightyWatt.SeriesResistance   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     *localclr-namespace:MightyWatt       � Title$  Series resistance�� � WindowStyle$ 
ToolWindow=� � 
ResizeMode$ NoResize=�$	��280��$	��200��.��� .+��� -   $��textBoxResistance��$��23��$
��Left=�$��8,8,0,0q�$	��Top=�$��60��$��1�� �� 	MaxLength$ 6���� -   $��buttonOK��$��OK��$��23��$
��Left=�$��	110,8,0,0q�$	��Top=�$��60�� �� 	IsDefault .  J� RenderTransformOrigin$ 2,-0.652&��� -   $��buttonCancel��$��Cancel��$��23��$
��Left=�$��	176,8,0,0q�$	��Top=�$��60�� �� IsCancel . �� -    ��Name$ 	textBlock��$��
6,36,10,10q�$
��Wrap=�$����This feature allows specifing a resistance that is connected in series with MightyWatt. It can be cable resistance or deliberately added power resistor. The value is only used to recalculate the allowable power dissipation in remote mode. No visible or logged values are affected. Use this feature with caution.��$
��Auto=��� -    ��Name$ 	labelUnit��$��Ω��$
��Left=�$��69,4,0,0q�$	��Top=�