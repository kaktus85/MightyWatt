   M S B A M L   `   `   `  ����   
MightyWatt    MightyWatt.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -   $��
mainWindow��  � Title$  MightyWatt Control��$	��470��$	��646��$	��244��$	��646�� � Icon$0 */MightyWatt;component/Resources/mwicon.ico��.��� .+��� -   $��	statusBar��$��23��$��Bottom=�.@��� -   $��statusBarFilePath��$
��Left=��� �� -   $��statusBarTemperature��$��Temperature: 25 °C��$��Center=��� �� -   $��statusBarConnection��$��Not connected��$��Right=�o� -   $��menuTop��$��23��$	��Top=�.@�m� -   $��menuItemConnection��$��
Connection��.@�m� -    m�Name$ menuItemConnectionZero��$ ��Arduino Zero (native port)��m� -	   $ menuItemConnectionUno��$*��$Arduino Uno & Due (programming port)��m� -
   $ menuItemDisconnect��$��
Disconnect��{��� *IsConnected+m� -   $��menuItemExperiment��$��Program items��.@�m� -   $��menuItemExperimentSave��$
��Save���� m� -   $��menuItemExperimentAdd��$	��Add��m� -   $��menuItemExperimentReplace��$��Replace��m� -   $��menuItemLogging��$��Logging��.@�m� -   $��menuItemLoggingNewFile��$��New File��m� -   $��menuItemLoggingCloseFile��$��
Close File���� m� -   $��menuItemLogManual��$��
Log Manual�� m� IsCheckable . m� -   $��menuItemLogProgram��$��Log Program�� . �� m� -   $��menuItemLoggingSettings��$��Settings��m� -   $��menuItemAdvanced��$��Advanced��.@�m� -   $��menuItemSeriesResistance��$��Series resistance��m� -   $��menuItemTools��$��Tools��.@�m� -   $+��%menuItemToolsIntegratorsAndStatistics��$ ��Integrators and statistics�� � ToolTip$: 4Charge and dissipated energy integrators, statistics��m� -   $��menuItemHelp��$
��Help��.@�m� -   $��menuItemDeviceInfo��$��Device Info��m� -   $��menuItemAbout��$��About��m� -   $��menuItemResources��$#��Open resource page in browser���� -   $��	groupBox2��$��Program��$��12,182,6,31q�.��� $
��Auto��.+��� -   $��checkBoxProgramLoop��$
��Loop��$
��Left=�$��	11,0,0,38q�$��16��$��Bottom=��� -   $��textBoxProgramLoopCount��$��23��$
��Left=�$��	91,0,0,34q�$��Bottom=�$��60��$�� ��{�.  �� -    $��label1��$��times��$��28��$
��Left=�$��
151,0,0,31q�$��Bottom=��� -!   $��listBoxProgram��$��6,10,324,63q�����  � ContextMenu �� .@�m� -"   $%��listBoxProgramContextMenuRemove��$��Remove��m� -#   $(��"listBoxProgramContextMenuRemoveAll��$��
Remove all�� � 	Resources  �� Color��%�   (��       * SystemColors.HighlightBrushKey+)(��      *SystemColors.ControlBrushKey+)�� $ 	#FF2624D7���� $ 	#322624D7���� -$   $��buttonProgramStart��$��Start��$��23��$
��Left=�$��6,0,0,6q�$��Bottom=�$��60���� -%   $��buttonProgramAdd��$	��Add��$��23��$
��Left=�$��72,0,0,6q�$��Bottom=�$��60���� -&   $��buttonProgramUp��$��˄��$��23��$
��Left=�$��	138,0,0,6q�$��Bottom=�$��30���� -'   $��buttonProgramDown��$��˅��$��23��$
��Left=�$��	174,0,0,6q�$��Bottom=�$��30���� -(   $��buttonProgramRemove��$��Remove��$��23��$
��Left=�$��	210,0,0,6q�$��Bottom=�$��60���� -)   $��tabControlProgram��$��0,10,6,6q�$��Right=�$	��309��.@��� -*   $��tabItemProgramConstant��$��Constant��.��� $��Stretch=�$��Stretch=�.+��� -+   $%��comboBoxProgramConstantQuantity��$��23��$
��Left=�$��6,6,0,0q� �� SelectedIndex$ 0��$	��Top=�$	��160��.@��� $��Inverted phase voltage���� -,   $��labelProgramConstantValue��$��Value��$��28��$
��Left=�$��6,33,0,0q�$	��Top=��� --   $"��labelProgramConstantDuration��$��Duration��$��28��$
��Left=�$��6,61,0,0q�$	��Top=��� -.   $!��textBoxProgramConstantValue��$��23��$
��Left=�$��	76,35,0,0q�$��0��$	��Top=�$��60��	 �� 	MaxLength$	 6���� -/   $$��textBoxProgramConstantDuration��$��23��$
��Left=�$��	76,63,0,0q�$��0��$	��Top=�$��60��$	 6���� -0   $!��comboBoxProgramConstantUnit��$��23��$
��Left=�$��
146,63,0,0q�$	��Top=�$��60���� -1   $��labelProgramConstantUnit��$
��Unit��$��28��$
��Left=�$��Center=�$��
136,33,0,0q�$	��Top=�$��23���� -2   $%��checkBoxProgramConstantPrevious��$��Previous��$��16��$
��Left=�$��
167,38,0,0q�$	��Top=��� -3   $!��checkBoxProgramConstantSkip��$��Skip if��$��16��$
��Left=�$��	12,94,0,0q�$	��Top=��� -4   $)��#comboBoxProgramConstantSkipQuantity��$��23��$
��Left=�$��	76,91,0,0q�$ 0��$	��Top=�$��80��.@��� $��
Resistance���� -5   $+��%comboBoxProgramConstantSkipComparator��$��23��$
��Left=�$��
162,91,0,0q�$ 1��$	��Top=�$��45��.@��� $��<���� $��>���� -6   $%��textBoxProgramConstantSkipValue��$��23��$
��Left=�$��
213,91,0,0q�$��0��$	��Top=�$��60���� -7   $"��labelProgramConstantSkipUnit��$
��Unit��$��28��$
��Left=�$��Center=�$��
273,89,0,0q�$	��Top=�$��23���� -8   $��tabItemProgramRamp��$
��Ramp��.��� .+��� -9   $!��comboBoxProgramRampQuantity��$��23��$
��Left=�$��6,6,0,0q�$	��Top=�$	��160��$ 0��.@��� $��Inverted phase voltage���� -:   $"��labelProgramRampInitialValue��$��Intial value��$��28��$
��Left=�$��6,33,0,0q�$	��Top=��� -;   $ ��labelProgramRampFinalValue��$��Final value��$��28��$
��Left=�$��6,61,0,0q�$	��Top=��� -<   $��labelProgramRampDuration��$��Duration��$��28��$
��Left=�$��6,89,0,0q�$	��Top=��� -=   $$��textBoxProgramRampInitialValue��$��23��$
��Left=�$��	76,35,0,0q�$��0��$	��Top=�$��60��$	 6���� ->   $"��textBoxProgramRampFinalValue��$��23��$
��Left=�$��	76,63,0,0q�$��0��$	��Top=�$��60��$	 6���� -?   $ ��textBoxProgramRampDuration��$��23��$
��Left=�$��	76,91,0,0q�$��0��$	��Top=�$��60��$	 6���� -@   $%��comboBoxProgramRampDurationUnit��$��23��$
��Left=�$��
146,91,0,0q�$	��Top=�$��60���� -A   $&�� labelProgramRampInitialValueUnit��$
��Unit��$��28��$
��Left=�$��
136,33,0,0q�$	��Top=�$��23��$��Center=��� -B   $$��labelProgramRampFinalValueUnit��$
��Unit��$��28��$
��Left=�$��
136,61,0,0q�$	��Top=�$��23��$��Center=��� -C   $!��checkBoxProgramRampPrevious��$��Previous��$��16��$
��Left=�$��
167,38,0,0q�$	��Top=��� -D   $��checkBoxProgramRampSkip��$��Skip if��$��16��$
��Left=�$��
12,122,0,0q�$	��Top=��� -E   $%��comboBoxProgramRampSkipQuantity��$��23��$
��Left=�$��
76,119,0,0q�$ 0��$	��Top=�$��80��.@��� $��
Resistance���� -F   $'��!comboBoxProgramRampSkipComparator��$��23��$
��Left=�$��162,119,0,0q�$ 1��$	��Top=�$��45��.@��� $��<���� $��>���� -G   $!��textBoxProgramRampSkipValue��$��23��$
��Left=�$��213,119,0,0q�$��0��$	��Top=�$��60���� -H   $��labelProgramRampSkipUnit��$
��Unit��$��28��$
��Left=�$��Center=�$��273,117,0,0q�$	��Top=�$��23��	� -I   $$��radioButtonProgramLoopInfinite��$��Infinite��$��16��$
��Left=�$��
205,0,0,38q�$��Bottom=�
 	� 	GroupName$

 loop��{�.  	� -J   $"��radioButtonProgramLoopFinite��$��16��$
��Left=�$��	74,0,0,36q�$��Bottom=�$

 loop�� d� IsThreeState .   d� 	IsChecked$
 TrueY�{�.  �� -K   $��groupBoxManualControl��$��Manual control��$��90��$��	12,28,0,0q�$	��Top=�$
��Left=�$	��272��.��� $
��Auto��.+��� -L   $��comboBoxManualQuantity��$��23��$
��Left=�$��6,9,0,0q�$ 0��$	��Top=�$	��168��.@��� $��Inverted phase voltage���� -M   $��buttonManualSet��$	��Set��$��23��$
��Left=�$��6,38,0,0q�$	��Top=�$��70���� -N   $��buttonManualStop��$
��Stop��$��23��$
��Left=�$��
104,37,0,0q�$	��Top=�$��70���� -O   $��textBoxManualValue��$��23��$
��Left=�$��	179,9,0,0q�$��0��$	��Top=�$��60��$	 6���� -P   $��labelManualUnit��$��X��$��28��$
��Left=�$��Center=�$��
239,7,-2,0q�$	��Top=�$��23���� -Q   $��	groupBox1��$��Values��$��90��$��
290,28,6,0q�$	��Top=�.��� $
��Auto��.+��� -R   $��labelValuesCurrent��$��Current��$
��Left=�$��6,7,0,0q�$��28��$	��Top=�$y�Visible=��� -S   $��labelValuesVoltage��$��Voltage��$��28��$
��Left=�$��	167,7,0,0q�$	��Top=��� -T   $��labelValuesPower��$��Power��$��28��$
��Left=�$��6,33,0,0q�$	��Top=��� -U   $��labelValuesResistance��$��
Resistance��$��28��$
��Left=�$��
167,33,0,0q�$	��Top=��� -V   $��textBox1��$
��Left=�$��
96,9,0,156q�$��60��$��1,582���� -W   $��textBoxValuesVoltage��$��23��$
��Left=�$��	232,9,0,0q�$��0��$	��Top=�$��60�� �� 
IsReadOnly . �� -X   $��textBoxValuesPower��$��23��$
��Left=�$��	70,35,0,0q�$��0��$	��Top=�$��60�� . �� -Y   $��textBoxValuesResistance��$��23��$
��Left=�$��
232,35,0,0q�$��0��$	��Top=�$��60�� . �� -Z   $��textBoxValuesCurrent��$��23��$
��Left=�$��70,9,0,0q�$��0��$	��Top=�$��60�� . �� -[   $��labelValuesA��$��A��$��28��$
��Left=�$��	130,7,0,0q�$	��Top=�$��23��$��Center=��� -\   $��labelValuesV��$��V��$��28��$
��Left=�$��	292,7,0,0q�$	��Top=�$��23��$��Center=��� -]   $��labelValuesW��$��W��$��28��$
��Left=�$��
130,33,0,0q�$	��Top=�$��23��$��Center=��� -^   $��labelValuesOhm��$��Ω��$��28��$
��Left=�$��
292,33,0,0q�$	��Top=�$��23��$��Center=��� -_   $��labelElapsed��$��Elapsed: 0 s of 0 s��$��0,-3,4,0q�$	��Top=�$��Right=�$y�Hidden=��� -`   $��groupBoxWatchdog��$��Watchdog��$��58��$��
12,121,0,0q�$	��Top=�$
��Left=�$	��312��.��� .+��� -a   $��checkBoxWatchdogEnable��$��Stop if��$��16��$
��Left=�$��12,9,0,0q�$	��Top=��� -b   $��comboBoxWatchdogQuantity��$��23��$
��Left=�$��77,6,0,0q�$ 0��$	��Top=�$��80��.@��� $��
Resistance���� -c   $ ��comboBoxWatchdogComparator��$��23��$
��Left=�$��	163,6,0,0q�$	��Top=�$��45��$ 1��.@��� $��<���� $��>���� -d   $��textBoxWatchdogValue��$��23��$��	214,6,0,0q�$	��Top=�$
��Left=�$��60��$��0���� -e   $��labelWatchdogUnit��$��W��$��28��$
��Left=�$��	274,4,0,0q�$	��Top=��� -f   $��groupBoxRemote��$��Voltage sense��$��58��$��330,121,6,0q�$	��Top=�.��� .+�	� -g   $��radioButtonLocal��$��Local (2-wire)��$��16��$
��Left=�$��12,9,0,0q�$	��Top=�$
 remote��$ FalseY�	� -h   $��radioButtonRemote��$��Remote (4-wire, Kelvin)��$��16��$
��Left=�$��	124,9,0,0q�$	��Top=�$
 remote��