using Godot;

namespace GodotServiceFramework.AI;

using System;
using System.Collections.Generic;
using System.Linq;

// 简单的神经网络实现
public class NeuralNetwork
{
    private Layer[] layers;
    private float learningRate = 0.1f;

    public NeuralNetwork(params int[] layerSizes)
    {
        layers = new Layer[layerSizes.Length - 1];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new Layer(layerSizes[i], layerSizes[i + 1]);
        }
    }

    public float[] Forward(float[] inputs)
    {
        float[] current = inputs;
        foreach (var layer in layers)
        {
            current = layer.Forward(current);
        }
        return current;
    }

    public void Train(float[] inputs, float[] targetOutputs)
    {
        // 前向传播
        var outputs = new float[layers.Length + 1][];
        outputs[0] = inputs;
        
        for (int i = 0; i < layers.Length; i++)
        {
            outputs[i + 1] = layers[i].Forward(outputs[i]);
        }

        // 反向传播
        var deltas = new float[layers.Length][];
        var lastLayer = layers.Length - 1;
        
        // 计算输出层的误差
        deltas[lastLayer] = new float[layers[lastLayer].OutputSize];
        for (int i = 0; i < deltas[lastLayer].Length; i++)
        {
            float output = outputs[lastLayer + 1][i];
            deltas[lastLayer][i] = (targetOutputs[i] - output) * output * (1 - output);
        }

        // 计算隐藏层的误差
        for (int i = lastLayer - 1; i >= 0; i--)
        {
            deltas[i] = new float[layers[i].OutputSize];
            for (int j = 0; j < layers[i].OutputSize; j++)
            {
                float sum = 0;
                for (int k = 0; k < layers[i + 1].OutputSize; k++)
                {
                    sum += deltas[i + 1][k] * layers[i + 1].weights[j, k];
                }
                deltas[i][j] = outputs[i + 1][j] * (1 - outputs[i + 1][j]) * sum;
            }
        }

        // 更新权重和偏置
        for (int i = 0; i < layers.Length; i++)
        {
            for (int j = 0; j < layers[i].InputSize; j++)
            {
                for (int k = 0; k < layers[i].OutputSize; k++)
                {
                    layers[i].weights[j, k] += learningRate * deltas[i][k] * outputs[i][j];
                }
            }
            
            for (int j = 0; j < layers[i].OutputSize; j++)
            {
                layers[i].biases[j] += learningRate * deltas[i][j];
            }
        }
    }

    // 保存模型
    public void SaveModel(string path)
    {
        using var writer = new System.IO.StreamWriter(path);
        foreach (var layer in layers)
        {
            // 保存权重
            for (int i = 0; i < layer.InputSize; i++)
            {
                for (int j = 0; j < layer.OutputSize; j++)
                {
                    writer.WriteLine(layer.weights[i, j]);
                }
            }
            // 保存偏置
            for (int i = 0; i < layer.OutputSize; i++)
            {
                writer.WriteLine(layer.biases[i]);
            }
        }
    }

    // 加载模型
    public void LoadModel(string path)
    {
        using var reader = new StreamReader(path);
        foreach (var layer in layers)
        {
            // 加载权重
            for (int i = 0; i < layer.InputSize; i++)
            {
                for (int j = 0; j < layer.OutputSize; j++)
                {
                    layer.weights[i, j] = float.Parse(reader.ReadLine()!);
                }
            }
            // 加载偏置
            for (int i = 0; i < layer.OutputSize; i++)
            {
                layer.biases[i] = float.Parse(reader.ReadLine()!);
            }
        }
    }
}

// 神经网络层
public class Layer
{
    public int InputSize { get; private set; }
    public int OutputSize { get; private set; }
    public float[,] weights;
    public float[] biases;
    private Random random = new Random();

    public Layer(int inputSize, int outputSize)
    {
        InputSize = inputSize;
        OutputSize = outputSize;
        weights = new float[inputSize, outputSize];
        biases = new float[outputSize];
        InitializeWeights();
    }

    private void InitializeWeights()
    {
        // Xavier初始化
        float scale = MathF.Sqrt(2.0f / (InputSize + OutputSize));
        for (int i = 0; i < InputSize; i++)
        {
            for (int j = 0; j < OutputSize; j++)
            {
                weights[i, j] = (float)(random.NextDouble() * 2 - 1) * scale;
            }
        }
        for (int i = 0; i < OutputSize; i++)
        {
            biases[i] = 0;
        }
    }

    public float[] Forward(float[] inputs)
    {
        var outputs = new float[OutputSize];
        for (int i = 0; i < OutputSize; i++)
        {
            float sum = biases[i];
            for (int j = 0; j < InputSize; j++)
            {
                sum += inputs[j] * weights[j, i];
            }
            outputs[i] = Sigmoid(sum);
        }
        return outputs;
    }

    private float Sigmoid(float x)
    {
        return 1f / (1f + MathF.Exp(-x));
    }
}

// 游戏AI控制器
public partial class GameAIController : Node
{
    private NeuralNetwork? network;
    private readonly List<TrainingExample> replayBuffer = [];
    private int _maxBufferSize = 1000;
    private int _batchSize = 32;
    
    public override void _Ready()
    {
        // 创建网络：4个输入 (生命值,距离,敌人数量,弹药), 8个隐藏节点, 4个输出 (攻击,防守,逃跑,补给)
        network = new NeuralNetwork(4, 8, 4);
        
        // 尝试加载已有模型
        try
        {
            network.LoadModel("user://game_ai_model.dat");
        }
        catch
        {
            GD.Print("No existing model found, starting fresh");
        }
    }
    
    public void CollectExperience(float[] state, float[] action, float reward)
    {
        replayBuffer.Add(new TrainingExample(state, action, reward));
        if (replayBuffer.Count > _maxBufferSize)
        {
            replayBuffer.RemoveAt(0);
        }
        
        // 当收集足够样本时进行训练
        if (replayBuffer.Count >= _batchSize)
        {
            TrainOnBatch();
        }
    }
    
    private void TrainOnBatch()
    {
        // 随机选择批次进行训练
        var batch = replayBuffer.OrderBy(x => Guid.NewGuid()).Take(_batchSize);
        foreach (var example in batch)
        {
            network!.Train(example.State, example.Action);
        }
    }
    
    public float[] GetAction(float[] state)
    {
        return network!.Forward(state);
    }
    
    public void SaveModel()
    {
        network!.SaveModel("user://game_ai_model.dat");
    }
}

// 训练样本类
public class TrainingExample
{
    public float[] State { get; private set; }
    public float[] Action { get; private set; }
    public float Reward { get; private set; }
    
    public TrainingExample(float[] state, float[] action, float reward)
    {
        State = state;
        Action = action;
        Reward = reward;
    }
}