# HummingbirdAgent

A reinforcement learning project with Unity ML-Agent.
![Image of training](/results/result.GIF)

## Training with python

It is important that the version of your training code matches the version in your Unity project. If you updated one, but not the other, training will fail.

cheak env

``` shell
mlagents-learn -h
```

``` shell
mlagents-learn ./config/Hummingbird.yaml --run-id hb_01
```

Here’s a breakdown of the different parts of the command:

`mlagents-learn`: The Python program that runs training

`./config/Hummingbird.yaml`: A relative path to the configuration file (this can also be a direct path)

`--run-id hb_01`: A unique name we choose to give this round of training (you can make this whatever you want)

Run the command.
When prompted, press Play in the Unity Editor to start training.


## Reference

[Unity ML-agent](https://github.com/Unity-Technologies/ml-agents)

[ML-Agents Setup (by immersivelimit)](https://www.immersivelimit.com/tutorials/unity-ml-agents-setup)

[ML-Agents Setup Anaconda (by immersivelimit)](https://www.immersivelimit.com/tutorials/ml-agents-python-setup-anaconda)