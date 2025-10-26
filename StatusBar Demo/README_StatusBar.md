# Hướng dẫn sử dụng ricaun.Revit.UI.StatusBar

## Giới thiệu
**ricaun.Revit.UI.StatusBar** là một thư viện NuGet giúp tạo progress bar trên StatusBar của Revit, giúp người dùng theo dõi tiến trình xử lý của các command.

## Các tính năng chính

### 1. RevitProgressBarUtils - Cách đơn giản nhất

#### Demo với số lần lặp:
```csharp
using ricaun.Revit.UI.StatusBar;

// Progress bar với số lần lặp cố định
int repeat = 100;
RevitProgressBarUtils.Run("Processing", repeat, (i) =>
{
    // Code xử lý của bạn
    System.Threading.Thread.Sleep(20);
    System.Console.WriteLine($"Step {i}");
});
```

#### Demo với collection:
```csharp
// Progress bar với collection của elements
var elements = new FilteredElementCollector(doc)
    .OfClass(typeof(Wall))
    .WhereElementIsNotElementType()
    .ToList();

RevitProgressBarUtils.Run("Processing Walls", elements, (wall) =>
{
    // Code xử lý từng wall
    System.Console.WriteLine($"Processing: {wall.Name}");
});
```

### 2. RevitProgressBar - Kiểm soát thủ công

#### Cách sử dụng cơ bản:
```csharp
using (var progressBar = new RevitProgressBar())
{
    progressBar.SetCurrentOperation("Collecting Elements");
    
    foreach (var element in elements)
    {
        // Tăng progress sau mỗi bước
        progressBar.Increment();
        
        // Code xử lý
        ProcessElement(element);
    }
}
```

#### Cách sử dụng với nhiều giai đoạn:
```csharp
using (var progressBar = new RevitProgressBar())
{
    // Stage 1
    progressBar.SetCurrentOperation("Stage 1: Collecting");
    for (int i = 0; i < 20; i++)
    {
        progressBar.Increment();
        // Do work...
    }
    
    // Stage 2
    progressBar.SetCurrentOperation("Stage 2: Processing");
    foreach (var item in items)
    {
        progressBar.Increment();
        // Do work...
    }
    
    // Stage 3
    progressBar.SetCurrentOperation("Stage 3: Finalizing");
    for (int i = 0; i < 10; i++)
    {
        progressBar.Increment();
        // Do work...
    }
}
```

### 3. BalloonUtils - Thông báo balloon

```csharp
using ricaun.Revit.UI.StatusBar;

// Hiển thị thông báo balloon trên Revit UI
BalloonUtils.Show("Task completed successfully!", "My Tool");
```

## Ví dụ thực tế trong project

File: `StatusBar Demo\StatusBarDemoCmd.cs`

Command này demo 4 cách sử dụng khác nhau:

1. **DemoSimpleProgress()**: Progress đơn giản với số lần lặp
2. **DemoElementsProgress()**: Progress với collection elements
3. **DemoManualProgress()**: Kiểm soát thủ công progress bar
4. **BalloonUtils**: Hiển thị thông báo khi hoàn thành

## Cách test

1. Build project
2. Load add-in vào Revit
3. Tìm button "StatusBar Demo" trong panel "Modify" của tab "Quoc_MEP"
4. Click button và quan sát StatusBar ở **dưới cùng** màn hình Revit

## Lưu ý quan trọng

### ✅ Nên làm:
- Sử dụng `using` statement với `RevitProgressBar` để tự động dispose
- Đặt tên operation mô tả rõ ràng cho người dùng hiểu
- Gọi `Increment()` sau mỗi bước xử lý
- Sử dụng `RevitProgressBarUtils` cho các trường hợp đơn giản

### ❌ Không nên:
- Quên dispose `RevitProgressBar` (sẽ để lại progress bar trên UI)
- Update quá nhanh (dưới 10ms) - người dùng sẽ không thấy được
- Nested progress bars (tạo nhiều progress bar cùng lúc)

## Khi nào nên dùng?

### Dùng RevitProgressBarUtils khi:
- Bạn biết chính xác số lần lặp hoặc số lượng elements
- Muốn code đơn giản, ngắn gọn
- Xử lý tuần tự, không có nhiều giai đoạn

### Dùng RevitProgressBar khi:
- Cần kiểm soát chi tiết từng bước
- Có nhiều giai đoạn xử lý khác nhau
- Muốn thay đổi operation text trong quá trình chạy
- Logic xử lý phức tạp

## Tham khảo thêm

- GitHub: https://github.com/ricaun-io/ricaun.Revit.UI.StatusBar
- NuGet: https://www.nuget.org/packages/ricaun.Revit.UI.StatusBar

## Tích hợp vào code hiện có

Ví dụ với command thay đổi độ dài pipe:

```csharp
// Trước khi có StatusBar
foreach (var pipe in pipes)
{
    ModifyPipeLength(pipe, newLength);
}

// Sau khi thêm StatusBar
RevitProgressBarUtils.Run("Changing pipe lengths", pipes, (pipe) =>
{
    ModifyPipeLength(pipe, newLength);
});
```

Đơn giản như vậy! 🎉
