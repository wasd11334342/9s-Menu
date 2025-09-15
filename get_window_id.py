import win32gui
import win32process
import psutil
import ctypes
import re

# 定義目標程序名稱
TARGET_PROCESS_NAME = "9s.exe"
# 雖然肉眼看每個程式只有一個視窗，但是一個視窗可能由多個視窗組成，所以用以下清單把不要的視窗名稱過濾掉
title_list = ['Program Manager','Desktop','Dummy','Msg','Promotion','Test','設置','MSCTFIME UI','Default IME','GDI+ Window (ONLINE.EXE)']
def get_window_title(hwnd):
    """獲取視窗標題"""
    try:
        return win32gui.GetWindowText(hwnd)
    except Exception:
        # 有些視窗可能因為權限或其他原因無法獲取標題
        return "[無法獲取標題]"

def is_window_visible(hwnd):
    """檢查視窗是否可見"""
    try:
        return win32gui.IsWindowVisible(hwnd)
    except Exception:
        return False # 若發生錯誤，視為不可見

def get_socketID_from_dll():
    dll = ctypes.CDLL("./dll_test.dll")
    dll.GetOnlineSocketHandles.restype = ctypes.c_char_p
    # 7/2發現如果霸主網路斷線的話，會有些字無法轉成utf-8
    result = dll.GetOnlineSocketHandles().decode("utf-8")

    pattern = r"\[PID (\d+)\]\s+Handle=(\d+)"
    return re.findall(pattern, result)
    

def find_online_exe_info():
    """尋找 online.exe 的 PID 及其所有視窗資訊"""
    online_pids = []
    online_windows_info = {}

    # 1. 尋找 online.exe 的 PID
    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'] == TARGET_PROCESS_NAME:
            online_pids.append(proc.info['pid'])

    if not online_pids:
        return online_pids, online_windows_info # 如果沒找到 online.exe，直接返回

    # print(f"找到 {TARGET_PROCESS_NAME} 的 PID(s): {online_pids}")

    # 2. 遍歷所有頂層視窗，找出屬於 online.exe 的視窗
    def enum_windows_callback(hwnd, extra):
        # 獲取視窗所屬的程序 ID
        tid, pid = win32process.GetWindowThreadProcessId(hwnd)

        if pid in online_pids:
            title = get_window_title(hwnd)
            is_visible = is_window_visible(hwnd)
            status = "visible" if is_visible else "invisible"
            
            # 確保視窗有標題，且不是桌面或Progman等系統視窗
            if title and title not in title_list:
                online_windows_info[pid]={
                    "hwnd": hwnd,
                    "title": title,
                    "status": status,
                    "handle": ""
                }
        return True # 繼續枚舉下一個視窗

    win32gui.EnumWindows(enum_windows_callback, None)

    matches = get_socketID_from_dll()
    for process in matches:
        if int(process[0]) in online_windows_info.keys():
            online_windows_info[int(process[0])]['handle'] = str(process[1])

    return online_pids, online_windows_info

if __name__ == "__main__":
    pids, windows_info = find_online_exe_info()
    print(windows_info)
    print("\n--- 搜尋結果 ---")
    if not pids:
        print(f"系統中未找到程序: {TARGET_PROCESS_NAME}")
    else:
        print(f"找到 {len(pids)} 個 {TARGET_PROCESS_NAME} 的實例。")
        print("\n--- 視窗資訊 ---")
        
        # 定義每個欄位的最大寬度，你可以根據實際輸出的內容調整
        PID_WIDTH = 7    # PID 通常不會太長，例如 5 位數
        HWND_WIDTH = 10  # HWND 是數字，約 8-10 位數
        STATUS_WIDTH = 9 # "visible" 或 "invisible"
        TITLE_MAX_WIDTH = 30 # 視窗標題可能會很長，設定一個最大長度並截斷
        
        # 列印標頭，用於對齊
        print(f"    {'PID':<{PID_WIDTH}} | {'HWND':<{HWND_WIDTH}} | {'status':<{STATUS_WIDTH}} | {'title':<{TITLE_MAX_WIDTH}}")
        print(f"    {'-' * PID_WIDTH} | {'-' * HWND_WIDTH} | {'-' * STATUS_WIDTH} | {'-' * TITLE_MAX_WIDTH}")

        if windows_info:
            for win in windows_info:
                # 格式化輸出
                # < 表示左對齊， > 表示右對齊
                # .<LENGTH 表示截斷到指定長度並左對齊
                formatted_title = windows_info[win]['title']
                if len(formatted_title) > TITLE_MAX_WIDTH:
                    formatted_title = formatted_title[:TITLE_MAX_WIDTH-3] + "..." # 截斷並加省略號

                print(f"    {win:<{PID_WIDTH}} | {windows_info[win]['hwnd']:<{HWND_WIDTH}} | {windows_info[win]['status']:<{STATUS_WIDTH}} | {formatted_title:<{TITLE_MAX_WIDTH}}")
        else:
            print(f"    未找到 {TARGET_PROCESS_NAME} 相關的任何有標題的視窗 (包括隱藏的)。")

    # print("\n------------------")
    # print("請注意：")
    # print("1. '隱藏' 指的是視窗的 IsWindowVisible 狀態為 False。")
    # print("2. 有些視窗可能因為系統權限或本身設計而無法獲取標題或狀態。")
    # print("3. 並非所有程序都有可見或隱藏的視窗。")