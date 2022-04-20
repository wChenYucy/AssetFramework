using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class MehtondExtension
{
    /// <summary>
    /// 判断一个字符串是否在一个字符串列表中
    /// </summary>
    /// <param name="str">要判断的字符串</param>
    /// <param name="strs">字符串数组</param>
    /// <returns></returns>
    public static bool IsStrInList(this string[] strs, string str)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (str == strs[i])
                return true;
        }

        return false;
    }
}
