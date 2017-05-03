﻿using System;
using System.Threading;

class SleepToTarget
{
    private DateTime TargetTime;
    private Action MyAction;
    private const int MinSleepMilliseconds = 250;

    public SleepToTarget(DateTime TargetTime, Action MyAction) {
        this.TargetTime = TargetTime;
        this.MyAction = MyAction;
    }

    public void Start() {
        new Thread(new ThreadStart(ProcessTimer)).Start();
    }

    private void ProcessTimer() {
        DateTime Now = DateTime.Now;

        while (Now < TargetTime) {
            int SleepMilliseconds = (int) Math.Round((TargetTime - Now).TotalMilliseconds / 2);
            Console.WriteLine(SleepMilliseconds);
            Thread.Sleep(SleepMilliseconds > MinSleepMilliseconds ? SleepMilliseconds : MinSleepMilliseconds);
            Now = DateTime.Now;
        }

        MyAction();
    }
}

