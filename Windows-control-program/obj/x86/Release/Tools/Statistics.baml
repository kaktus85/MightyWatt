   M S B A M L   `   `   `  ���� E  AMightyWatt, Version=1.3.1.0, Culture=neutral, PublicKeyToken=null'clr-namespace:MightyWatt
MightyWatt      MightyWatt.Statistics   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     *localclr-namespace:MightyWatt       � Title$   Integrators and statistics��$	��500��$	��520��$	��520��$	��520��$	��170�� � Icon$0 */MightyWatt;component/Resources/mwicon.ico��.��� .+��� -    ��Name$ groupBoxIntegrators��$��Integrators��$
��Left=�$��	10,10,0,0q�$	��Top=�$	��115��$	��340��.��� $��0,0,0,0q�.+��� -    ��Name$ labelCharge��$��Charge��$
��Left=�$��6,7,0,0q�$	��Top=��� -   $ labelEnergy��$��Dissipated energy��$
��Left=�$��6,34,0,0q�$	��Top=��� -    ��Name$ textBoxCharge��$
��Left=�$��23��$��	115,9,0,0q� �� TextWrapping$
 Wrap=�$	��Top=�$	��100������ *Charge+ �� Mode$ OneWay=��� -    ��Name$ comboBoxChargeUnit��$
��Left=�$��
220,9,-2,0q�$	��Top=�$	��100�� �� SelectedIndex$ 0��$��23��.@��� $��C���� $��Ah���� -   $ textBoxEnergy��$
��Left=�$��23��$��
115,36,0,0q�$
 Wrap=�$	��Top=�$	��100������ *Energy+$ OneWay=��� -   $ comboBoxEnergyUnit��$
��Left=�$��220,36,-2,0q�$	��Top=�$	��100��$ 0��$��23��.@��� $��J���� $��Wh���� -   $ 	labelTime��$
��Time��$
��Left=�$��6,61,0,0q�$	��Top=��� -	   $ textBoxTime��$
��Left=�$��23��$��
115,63,0,0q�$
 Wrap=�$	��Top=�$	��100������ *Time+$ OneWay=��� -
   $ comboBoxTimeUnit��$
��Left=�$��220,63,-2,0q�$	��Top=�$	��100��$ 0��$��23��.@��� $��s���� $��hh:mm:ss.sss���� -   $ groupBoxAverages��$��
Statistics��$
��Left=�$��
10,130,0,0q�$	��Top=�$	��160��$	��485��.��� $��0,0,0,0q�.+��� -   $ labelVoltage��$��Voltage [V]��$
��Left=�$��6,56,0,0q�$	��Top=��� -   $ textBoxVoltage��$
��Left=�$��23��$��
105,58,0,0q�$
 Wrap=�$	��Top=�$��85������ *VoltageAverage+$ OneWay=��� -   $ labelCurrent��$��Current [A]��$
��Left=�$��6,30,0,0q�$	��Top=��� -   $ textBoxCurrent��$
��Left=�$��23��$��
105,32,0,0q�$
 Wrap=�$	��Top=�$��85������ *CurrentAverage+$ OneWay=��� -   $ 
labelPower��$��	Power [W]��$
��Left=�$��6,82,0,0q�$	��Top=��� -   $ textBoxPower��$
��Left=�$��23��$��
105,84,0,0q�$
 Wrap=�$	��Top=�$��85������ *PowerAverage+$ OneWay=��� -   $ labelResistance��$��Resistance [Ω]��$
��Left=�$��	6,108,0,0q�$	��Top=��� -   $ textBoxResistance��$
��Left=�$��23��$��105,110,0,0q�$
 Wrap=�$	��Top=�$��85������ *ResistanceAverage+$ OneWay=��� -   $ textBoxCurrentSD��$
��Left=�$��23��$��
195,32,0,0q�$
 Wrap=�$	��Top=�$��85������ *	CurrentSD+$ OneWay=��� -   $ labelSD��$��SD��$
��Left=�$��	200,5,0,0q�$	��Top=�	 � ToolTip$	 Standard deviation���� -   $ labelMinimum��$��Minimum��$
��Left=�$��	290,5,0,0q�$	��Top=��� -   $ labelMaximum��$��Maximum��$
��Left=�$��	380,5,0,0q�$	��Top=��� -   $ textBoxCurrentMinimum��$
��Left=�$��23��$��
285,32,0,0q�$
 Wrap=�$	��Top=�$��85������ *
CurrentMin+$ OneWay=��� -   $ textBoxCurrentMaximum��$
��Left=�$��23��$��
375,32,0,0q�$
 Wrap=�$	��Top=�$��85������ *
CurrentMax+$ OneWay=��� -   $ labelAverage��$��Average��$
��Left=�$��	110,5,0,0q�$	��Top=��� -   $ textBoxVoltageSD��$
��Left=�$��23��$��
195,58,0,0q�$
 Wrap=�$	��Top=�$��85������ *	VoltageSD+$ OneWay=��� -   $ textBoxVoltageMinimum��$
��Left=�$��23��$��
285,58,0,0q�$
 Wrap=�$	��Top=�$��85������ *
VoltageMin+$ OneWay=��� -   $ textBoxVoltageMaximum��$
��Left=�$��23��$��
375,58,0,0q�$
 Wrap=�$	��Top=�$��85������ *
VoltageMax+$ OneWay=��� -   $ textBoxPowerSD��$
��Left=�$��23��$��
195,84,0,0q�$
 Wrap=�$	��Top=�$��85������ *	PowerSD+$ OneWay=��� -   $ textBoxPowerMinimum��$
��Left=�$��23��$��
285,84,0,0q�$
 Wrap=�$	��Top=�$��85������ *
PowerMin+$ OneWay=��� -    $ textBoxPowerMaximum��$
��Left=�$��23��$��
375,84,0,0q�$
 Wrap=�$	��Top=�$��85������ *
PowerMax+$ OneWay=��� -!   $ textBoxResistanceSD��$
��Left=�$��23��$��195,110,0,0q�$
 Wrap=�$	��Top=�$��85������ *ResistanceSD+$ OneWay=��� -"   $ textBoxResistanceMinimum��$
��Left=�$��23��$��285,110,0,0q�$
 Wrap=�$	��Top=�$��85������ *ResistanceMin+$ OneWay=��� -#   $ textBoxResistanceMaximum��$
��Left=�$��23��$��375,110,0,0q�$
 Wrap=�$	��Top=�$��85������ *ResistanceMax+$ OneWay=��� -$   $ groupBoxLog��$	��Log��$
��Left=�$��10,295,0,12q�$	��485��.��� $��0,0,0,0q�.+��� -%   
 ��Name$
 	buttonLog��$��Log snapshot��$
��Left=�$��
367,10,0,0q�$	��Top=�$��90��{��� *LogFileAvailable+$ OneWay=��� -&    ��Name$ checkBoxOffset��$��
Offset log��$
��Left=�$��
275,13,0,0q�$	��Top=�$	 Log outside data columns�� d� 	IsChecked �� *	OffsetLog+$ TwoWay=��� -'   $ textBoxUserNote��$
��Left=�$��
10,35,0,10q�$
 Wrap=�$	��453���� -(   $ labelUserNote��$��User note (optional)��$
��Left=�$��6,7,0,0q�$	��Top=��� -)   $ labelLogged��$��Log saved…��$
��Left=�$��	160,7,0,0q�$	��Top=����	#FF1D59A2y��� *LogSavedNotification+$ OneWay=��� -*   $ groupBoxControl��$��Control��$
��Left=�$��
355,10,0,0q�$	��Top=�$	��115��$	��140��.��� $��0,0,0,0q�.+��� -+   $
 buttonReset��$��Reset��$��	10,55,8,0q�$	��Top=�$��30���� -,   $
 buttonStartStop��$��10,9,8,0q�$	��Top=�$��30������ *	StartStop+$ OneWay=� �� UpdateSourceTrigger$ PropertyChanged=�