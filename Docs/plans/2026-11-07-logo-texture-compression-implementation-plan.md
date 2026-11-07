# Làm nhẹ ảnh logo màn loading — Implementation Plan

**Ngày:** 2026-11-07
**Design:** [2026-11-07-startup-loading-delay-design.md](../designs/2026-11-07-startup-loading-delay-design.md), mục 3.3
**Trạng thái:** Đã triển khai và đo xong; chờ kiểm tra logo bằng mắt trên máy thật (bước 5).

## Kết quả đo

| Chỉ số | Trước | Sau |
|---|---|---|
| `Combo_Block.png` trong Build Report | 11,1 MB | 342,4 KB |
| Lần đặt orientation cuối → `REQUEST_PERMISSIONS` | 3,50–3,79 giây (trung bình 3,64) | 3,00–3,69 giây (trung bình 3,45) |
| Bấm mở app → `REQUEST_PERMISSIONS` | 6,48–7,76 giây (trung bình 7,18) | 5,32–8,11 giây (trung bình 6,59) |

Mỗi bên đo 5 lần trên M2101K7BG. Bên "trước" mở tay từ launcher, bên "sau" mở bằng `adb monkey`, nên chênh lệch nhỏ chưa đủ để kết luận. Khoảng chờ chính gần như không đổi: ảnh logo không phải nguyên nhân chính.

## Phạm vi

Chỉ đổi import settings của `Combo_Block.png` trên Android và iOS. Việc xin quyền thông báo đang tạm dừng và không nằm trong plan này.

## File bị ảnh hưởng

| File | Thay đổi |
|---|---|
| `Assets/_Game/_BlockDrag/Resource/Combo_Block.png.meta` | Thêm override cho Android và iPhone |

Không đổi file PNG, GUID, prefab, scene, code hay package. `InitLoadingCanvas` và `HomeCanvas` tiếp tục tham chiếu sprite cũ.

## Các bước

### 1. Đổi import settings

Dùng `TextureImporter` trong Unity Editor để Unity tự ghi `.meta` và reimport, không sửa `.meta` bằng tay. Áp dụng cho cả `Android` và `iPhone`:

| Thuộc tính | Giá trị |
|---|---|
| `overridden` | `true` |
| `format` | `TextureImporterFormat.ASTC_6x6` |
| `maxTextureSize` | `1024` |

Giữ nguyên thiết lập mặc định và Standalone.

### 2. Kiểm tra trong Editor

1. Đọc lại texture, xác nhận kích thước khoảng 1024×706 và định dạng ASTC 6x6 khi target là Android.
2. Đọc console, xác nhận không có lỗi hoặc cảnh báo mới từ texture này.
3. `git status` chỉ cho thấy file `.meta` trên và các file tài liệu của task này thay đổi.

### 3. Build lên máy test

Build Android và cài lên máy M2101K7BG đang kết nối. Lần build này phải rebuild player data để Build Report có danh sách asset.

### 4. Đo và so sánh

1. **Dung lượng:** tìm `Combo_Block.png` trong Build Report của `Editor.log`, kỳ vọng dưới 1 MB.
2. **Thời gian:** tắt hẳn app rồi mở lại 5 lần qua `adb`. Với mỗi lần, lấy từ logcat 2 đoạn sau và so với mốc cũ:

| Đoạn đo | Mốc cũ |
|---|---|
| Bấm mở app → `REQUEST_PERMISSIONS` | 6,5–7,8 giây |
| Lần đặt orientation cuối → `REQUEST_PERMISSIONS` | 3,5–3,8 giây |

Mốc đo này dùng được vì lệnh xin quyền vẫn chạy ở `BeforeSceneLoad`. Kết quả sẽ báo đúng như đo được, kể cả khi thời gian không giảm đáng kể.

### 5. Kiểm tra bằng mắt (bạn thực hiện)

Mở game trên máy thật và xem logo ở màn loading và màn Home. Nếu thấy mờ rõ rệt, nâng `maxTextureSize` lên 1536 hoặc 2048, vẫn giữ ASTC 6x6, rồi đo lại.

## Hoàn tác

Trả `Combo_Block.png.meta` về bản đã commit rồi để Unity reimport.

## Không làm trong plan này

- Không commit trừ khi bạn yêu cầu.
- Không đo Profiler. Nếu kết quả cho thấy vẫn chậm, mình sẽ báo lại để bạn quyết định bước tiếp theo.
