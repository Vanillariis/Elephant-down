using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class LlamaTesting : MonoBehaviour
{
    // Unity automatically adds "lib" prefix and ".so" suffix on Android, 
    // or ".dll" on Windows when looking for "llama"
    [DllImport("llama")]
    private static extern void llama_backend_init();

    [DllImport("llama")]
    private static extern IntPtr llama_print_system_info();

    void Start()
    {
        try
        {
            // 1. Initialize the backend
            llama_backend_init();
            Debug.Log("Llama Backend Initialized Successfully!");

            // 2. Get System Info (to see if Vulkan/ARM Neon is working)
            IntPtr infoPtr = llama_print_system_info();
            string info = Marshal.PtrToStringAnsi(infoPtr);
            Debug.Log("Llama System Info: " + info);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load Llama Library: " + e.Message);
        }
    }
}
