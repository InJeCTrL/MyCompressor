using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor
{
    /// <summary> 检查当前版本，若有发现新版本则提示更新
    /// </summary>
    class VersionCheck
    {
        /// <summary> 展示用主页地址
        /// </summary>
        String HomePage;
        /// <summary> 更新类初始化，引入展示主页地址
        /// </summary>
        /// <param name="URL"></param>
        public VersionCheck(String URL)
        {
            HomePage = URL;//地址记录
            Do_Check();//进行检查
        }
        /// <summary> 联网打开主页检查版本号，并提示是否更新
        /// </summary>
        private void Do_Check()
        {

        }
        /// <summary> 更新函数，调用下载函数、执行批处理函数
        /// </summary>
        private void Update()
        {

        }
        /// <summary> 从主页给出的地址下载新版本到临时文件夹
        /// </summary>
        private void Download()
        {

        }
        /// <summary> 释放批处理，解压缩剪切替换、打开新版本、自删除
        /// </summary>
        private void ReplaceBAT()
        {

        }
    }
}
