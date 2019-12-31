# 足球标注工具
**标注工具使用（vs，运行程序时会有使用说明）：**

Open-打开图片序列文件读入图片数据，自动初始化标记文件gtoundtruth.txt，label.txt， oc.txt；点击左侧图片序列第一帧，开始显示图片；开始逐帧标记：

*	up键选择上一帧 down键选择下一帧；
*	ad调整目标框的宽度w， ws调整目标框的高度h；
*	4658调整目标框左上角坐标；
*	勾选标框，开始标框；
*	勾选描点，开始描点；
* 标较小可以使用缩放，以便标注更准确。


## 具有以下功能：
>> 0. 初始时会生成groundtruth.txt、label.txt、oc.txt文件，并进行初始化。

>> 1. 对球员位置进行标框，保存至groundtruth.txt文件。

>> 2. 保存是否遮挡信息，保存到label.txt文件。0表示没有遮挡，1表示同队遮挡，2表示异队遮挡，3表示多人遮挡。

>> 3. 在存在遮挡的情况下，可以描点，表示球员中心坐标，并保存至oc.txt文件。

>> 4. 图片格式 xx.jpg，图片放在exe目录/output/image文件夹下。

![图一](https://github.com/834810071/footballer-label-tool/blob/master/TFLabelTool/readme/0.png)

![图二](https://github.com/834810071/footballer-label-tool/blob/master/TFLabelTool/readme/1.png)

![图三](https://github.com/834810071/footballer-label-tool/blob/master/TFLabelTool/readme/2.png)
