using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using UnityEditor;

[ExecuteAlways]
public class ThreadedDataRequester : MonoBehaviour {
    private static bool emptyDataQueueInUpdate = false;
    //the Queues that hold the data for the heightmaps, as well as meshes
    //the reason we use a queue is for the latter, as unity won't let you do stuff like alter meshes outside of the main thread.
    static Queue<ThreadResult> dataQueue = new Queue<ThreadResult>();

    //The threading works by passing in a method generateData, and a method to be done when that data has been generated
    public static void RequestData(Func<object> generateData, Action<object> callback) {
        ThreadPool.QueueUserWorkItem(DataThread, new ThreadInfo(generateData, callback));
    }

    private static void DataThread(object threadinfo) {
        ThreadInfo threadInfo = (ThreadInfo)threadinfo;
        //generate the data
        object data = threadInfo.generateData();

        //makes sure that only one thread can access the Queue at once, as they are not thread-safe
        lock (dataQueue) {
            //add the Info and callback(what to do with the info) to the Queue
            if (Application.isEditor) {
                if (!emptyDataQueueInUpdate) {
                    EditorApplication.update += emptyDataQueue;
                    emptyDataQueueInUpdate = true;
                }
            }
            dataQueue.Enqueue(new ThreadResult(threadInfo.callback, data));
        }
    }

    void Update() {
        //if there's stuff in the Queue, take it out and execute the callback
        if (dataQueue.Count > 0) {
            int count = dataQueue.Count;
            for (int i = 0; i < count; i++) {
                ThreadResult threadResult = dataQueue.Dequeue();
                threadResult.callback(threadResult.parameter);
            }
        }
    }

    private static void emptyDataQueue() {
        if (Application.isEditor) {
            EditorApplication.update -= emptyDataQueue;
            emptyDataQueueInUpdate = false;
        }
        if (dataQueue.Count > 0) {
            int count = dataQueue.Count;
            for (int i = 0; i < count; i++) {
                ThreadResult threadResult = dataQueue.Dequeue();
                threadResult.callback(threadResult.parameter);
            }
        }
    }

    struct ThreadResult {
        public readonly Action<object> callback;
        public readonly object parameter;
        public ThreadResult(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    struct ThreadInfo {
        public readonly Func<object> generateData;
        public readonly Action<object> callback;
        public ThreadInfo(Func<object> generateData, Action<object> callback) {
            this.generateData = generateData;
            this.callback = callback;
        }
    }
}
