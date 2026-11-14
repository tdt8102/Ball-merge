# Kỹ năng theo bộ mèo (Theme Set Skills)

> Trạng thái: **đề xuất thiết kế, chưa triển khai**. Tài liệu này để đọc và chốt các câu hỏi ở mục 10 trước khi viết code.
> Ý tưởng gốc: sưu tầm đủ mèo của một chủ đề thì mở khoá kỹ năng của chủ đề đó; mặc cả bộ thì dùng được kỹ năng trong game. Chủ đề mặc định (Cổ điển) không có kỹ năng.

---

## 1. Tóm tắt

| Bước | Điều kiện | Kết quả |
|---|---|---|
| **Mở khoá** | Sở hữu đủ **11/11** mèo của một chủ đề (nhờ Gacha) | Kỹ năng của chủ đề đó được mở khoá vĩnh viễn |
| **Kích hoạt** | Đang **mặc đủ 11 vị trí bằng đúng chủ đề đó** khi bắt đầu ván | Trong ván có nút kỹ năng, nạp năng lượng bằng cách gộp mèo |
| **Chủ đề Cổ điển** | – | Không có kỹ năng |
| **Mặc lẫn nhiều chủ đề** | – | Không có kỹ năng (đổi lại được tự do phối đồ) |

Điểm hay của luật này: nó tạo **một lựa chọn thật** cho người chơi. Phối mỗi con một bộ cho đẹp, hay mặc nguyên bộ để có kỹ năng. Và nó cho Gacha một mục tiêu rõ ràng hơn "gom cho đủ": hoàn thành một bộ.

Có **7 chủ đề có kỹ năng**: Nét vẽ, Dễ thương, Chibi, Pixel, Nửa đêm, Kẹo ngọt, Hoàng kim.

---

## 2. Luật chi tiết

### 2.1 Mở khoá
- Tính theo số mèo **đang sở hữu** của chủ đề (tính cả mèo đã có từ gacha, mảnh sao nếu làm mục 8).
- Mở khoá là vĩnh viễn, không mất nếu sau này đổi đồ.
- Lúc nhận con mèo cuối cùng của bộ (từ Gacha hoặc ghép mảnh), hiện màn **"HOÀN THÀNH BỘ! Mở khoá kỹ năng: …"** có hiệu ứng riêng.

### 2.2 Kích hoạt
- Điều kiện: kỹ năng đã mở khoá **và** cả 11 vị trí mèo đều đang mặc chủ đề đó.
- Kiểm tra **một lần lúc vào màn chơi** (và khi tiếp tục ván đang dở). Skin chỉ đổi được ở Menu nên không có chuyện đổi giữa ván.
- Nếu thiếu (ví dụ mặc 10/11): Skin tab hiện nhãn "Thiếu 1 con để dùng kỹ năng" và bấm vào là nhảy tới đúng vị trí còn thiếu.
- Nút **MẶC CẢ BỘ NÀY** hiện có sẽ trở thành đường tắt chính: một chạm là đủ 11 vị trí.

### 2.3 Bảng trạng thái của một chủ đề (hiển thị trên Skin và Sổ mèo)

| Trạng thái | Hiển thị | Ví dụ |
|---|---|---|
| Chưa đủ bộ | Biểu tượng khoá mờ + tiến độ | `Kỹ năng: Tẩy  🔒 7/11` |
| Đủ bộ, chưa mặc đủ | Biểu tượng sáng + nhắc | `Kỹ năng: Tẩy  ✔ mở khoá · đang mặc 9/11` |
| Đủ bộ và mặc đủ | Biểu tượng sáng, viền xanh | `Kỹ năng: Tẩy  ✔ ĐANG DÙNG` |

---

## 3. Cách dùng kỹ năng trong game

### 3.1 Năng lượng
- Thanh năng lượng 0–100%, nạp bằng cách **gộp mèo** (khuyến khích chơi hay, hợp với Suika):
  - mỗi lần gộp: **+6**
  - combo: **+3** cho mỗi bậc combo (combo 3 = +9 thêm)
  - gộp ra mèo cấp từ 6 trở lên: **+4** thêm
- Dùng kỹ năng tốn **100**. Ước lượng: khoảng **14–18 lần gộp** mới đầy, nên một ván trung bình dùng được 3–5 lần.
- Không nạp trong lúc kỹ năng đang chạy. Không nạp khi đã hết ván.
- Bắt đầu mỗi ván: **30%** (có thể dùng sớm nhưng không dùng ngay được).
- Thông số nằm trong `balance.json` để chỉnh không cần sửa code.

### 3.2 Nút kỹ năng
- Nút tròn có hình đầu mèo của chủ đề, vòng tiến độ chạy quanh viền. Đủ năng lượng thì viền sáng và nhấp nháy nhẹ.
- Vị trí đề xuất: **góc phải, dưới ô NEXT** (vùng trống phía trên khung chơi). Xem mục 7.
- Bấm nút khi chưa đủ năng lượng: rung nhẹ + thông báo "Chưa đủ năng lượng".

### 3.3 Hai loại kỹ năng
1. **Chạm để chọn mục tiêu** (Tẩy, Lên cấp, Midas): bấm nút → màn chơi tối nhẹ, mèo trong bể có thể chạm được, dòng gợi ý "Chạm một chú mèo". Bấm ra ngoài hoặc bấm nút lần nữa để **huỷ, không mất năng lượng**.
2. **Dùng ngay** (Cái ôm, Thu nhỏ, Ánh trăng, Cơn sốt đường): bấm là chạy, có đồng hồ đếm ngược quanh nút.

### 3.4 Tương tác với các phần khác
- **Tạm dừng / Cài đặt:** mọi bộ đếm giờ của kỹ năng dừng theo (dùng thời gian của game, không dùng giờ thật).
- **Hết ván:** nút bị khoá, hiệu ứng đang chạy dừng.
- **Ván làm lại / ván mới:** năng lượng về 30%.
- **Tiếp tục ván dở** (đã có lưu ván): lưu thêm năng lượng vào bản lưu. Hiệu ứng đang chạy dở thì bỏ, không lưu.
- **Kéo thả mèo:** đang ở chế độ chọn mục tiêu thì không thả mèo; chạm vào nút kỹ năng không được tính là thả mèo.

---

## 4. Bảy kỹ năng

Chia theo vai trò để mỗi bộ có "tính cách" riêng và không có bộ nào lấn át hẳn:

| Vai trò | Kỹ năng |
|---|---|
| Dọn bể | Tẩy (Nét vẽ), Midas (Hoàng kim) |
| Hỗ trợ gộp | Cái ôm (Dễ thương), Lên cấp (Pixel) |
| Phòng thủ | Ánh trăng (Nửa đêm), Thu nhỏ (Chibi) |
| Tăng thưởng | Cơn sốt đường (Kẹo ngọt) |

| Chủ đề | Kỹ năng | Loại | Hiệu ứng | Thông số đề xuất |
|---|---|---|---|---|
| **Nét vẽ** | **Tẩy** | Chọn mục tiêu | Xoá một chú mèo bất kỳ khỏi bể. Không cho điểm, không cho xu. | Mọi cấp |
| **Dễ thương** | **Cái ôm** | Dùng ngay | Các mèo **cùng cấp** ở gần nhau hút nhau, dễ chạm và gộp. | 3 giây, bán kính hút 3 đơn vị |
| **Chibi** | **Thu nhỏ** | Dùng ngay | Mọi mèo nhỏ lại, có thêm chỗ trống, rồi lớn dần về cũ. | ×0,85 trong 6 giây, lớn lại trong 1,5 giây |
| **Pixel** | **Lên cấp** | Chọn mục tiêu | Nâng một chú mèo **lên 1 cấp** ngay lập tức. | Chỉ mèo cấp 0–8 (kết quả tối đa cấp 9) |
| **Nửa đêm** | **Ánh trăng** | Dùng ngay | Tạm ngừng đếm giờ thua ở vạch đỏ. Vạch đổi sang màu xanh. | 8 giây, không đặt lại bộ đếm |
| **Kẹo ngọt** | **Cơn sốt đường** | Dùng ngay | Điểm và xu từ gộp mèo nhân hệ số trong một lúc. | 10 giây, điểm ×2, xu ×1,5 |
| **Hoàng kim** | **Midas** | Chọn mục tiêu | Biến một chú mèo thành vàng và **bán lấy xu**, mèo biến mất. | Mèo cấp 0–7, xu = `3 + 2×cấp` (3 đến 17) |

### 4.1 Chi tiết triển khai và rủi ro từng kỹ năng

**Tẩy (Nét vẽ)**
- Tìm mèo bằng chạm (`Physics2D.OverlapPoint`) rồi huỷ đối tượng, kèm hiệu ứng "bị gạch xoá".
- Rủi ro thấp. Có thể bị lạm dụng để xoá mèo lớn ở đáy bể nhằm "reset" — chấp nhận được vì tốn trọn một thanh năng lượng.

**Cái ôm (Dễ thương)**
- Mỗi khung hình, với mỗi cặp mèo cùng cấp trong bán kính, thêm một lực hút nhỏ theo hướng nối hai tâm (tỉ lệ khối lượng, có giới hạn lực tối đa).
- Hiệu năng: tối đa vài chục mèo nên so từng cặp vẫn nhẹ (đã tính; cần đo trên điện thoại yếu).
- Rủi ro: lực quá mạnh làm bể "nổ" hoặc mèo dính nhau rung. Cần chỉnh kỹ, bắt đầu từ lực nhỏ.
- Có thể gây gộp dây chuyền, tốt cho combo (và cho năng lượng).

**Thu nhỏ (Chibi)**
- Thu cả hình lẫn bán kính vùng va chạm của mọi mèo ×0,85, giữ 6 giây, rồi lớn lại **từ từ** (1,5 giây) để vật lý đẩy các mèo ra, tránh chồng lấn đột ngột.
- Rủi ro **trung bình**: lúc lớn lại có thể đẩy mèo trồi lên vạch đỏ hoặc rung. Cần thử kỹ. Cách giảm rủi ro: tạm dừng đếm giờ thua trong 1,5 giây lớn lại.
- Phương án thay thế nếu thấy khó chịu: **"Nảy lên"**, xung lực nhỏ hướng lên cho mọi mèo để xáo lại bể (cơ chế "lắc" quen thuộc của game Suika). Dễ làm và ổn định hơn.

**Lên cấp (Pixel)**
- Đổi cấp của mèo được chọn (cần cập nhật hình, bán kính, khối lượng), có hiệu ứng vỡ ô vuông pixel.
- Tính là mèo mới xuất hiện (cập nhật Sổ mèo và thưởng khám phá nếu là lần đầu).
- Cấp mục tiêu tối đa 8 để **không** nâng thẳng lên cấp 10 (Majesty). Con số này chỉnh được.
- Bán kính tăng có thể đẩy mèo xung quanh; chấp nhận, vật lý tự xử lý.

**Ánh trăng (Nửa đêm)**
- Bộ đếm hết ván ở `GameManager` (`_dangerTimer`) đứng yên trong 8 giây rồi chạy tiếp từ chỗ cũ.
- Không cho thắng, chỉ mua thời gian để gộp cho thấp xuống. Rủi ro thấp.
- Hiệu ứng: nền tối xanh, vạch đỏ chuyển xanh, vầng trăng ở góc.

**Cơn sốt đường (Kẹo ngọt)**
- Nhân điểm ×2 và xu ×1,5 cho các lần gộp trong 10 giây.
- **Chỉ áp dụng cho xu từ gộp**, không áp dụng cho thưởng khám phá mèo mới và không áp dụng cho hoàn tiền Gacha, để không làm lạm phát tiền.
- Cần xem lại sau khi đo thu nhập (xem mục 8).

**Midas (Hoàng kim)**
- Bán mèo lấy xu. Khác Tẩy ở chỗ **có xu nhưng chỉ bán được mèo nhỏ**, và xu bán được thấp.
- Rủi ro kinh tế: nếu cho bán mèo to với giá cao thì thành máy in tiền. Do đó giới hạn cấp 0–7 và giá thấp.

---

## 5. Cân bằng và đo đạc

- Mọi thông số nằm trong mục `skills` của `balance.json` (năng lượng mỗi lần gộp, thời lượng, hệ số, cấp tối đa...).
- Đề xuất ghi lại số liệu khi thử: số lần dùng kỹ năng mỗi ván, điểm trung bình khi có / không có kỹ năng, để biết kỹ năng nào quá mạnh hoặc không ai dùng.
- Nguyên tắc: kỹ năng nên **giúp thoát tình huống xấu hoặc thêm thưởng**, không biến ván thành bất bại.
  Chỉ số cần theo dõi là điểm trung bình tăng bao nhiêu phần trăm khi dùng bộ có kỹ năng. Mục tiêu gợi ý: tăng khoảng 10–20%, không hơn.

---

## 6. Gacha: bài toán độ khó "đủ bộ"

Đây là phần cần quyết định nhất. Luật "đủ 11/11" rất khó đạt với Gacha hiện tại.

Tôi mô phỏng Gacha đúng như đang chạy trong game (70% Thường, 25% Hiếm, 5% Huyền thoại, bảo hiểm Huyền thoại mỗi 30 lượt, mỗi món trong cùng một độ hiếm có xác suất ngang nhau), đếm số lượt quay để đủ 11 mèo của **một** chủ đề (4.000 lần mô phỏng):

| Kịch bản | Số lượt quay trung bình | Trung vị | 90% người chơi cần tối đa |
|---|---|---|---|
| **Hiện tại** (giá 150 xu/lượt) | **464** | 380 | 835 |
| Tỉ lệ 60/30/10, bảo hiểm 30 | 326 | 290 | 529 |
| Bảo hiểm 20 lượt | 399 | 340 | 695 |

464 lượt ≈ **70.000 xu** (trừ đi khoảng 17.000 xu hoàn lại khi trùng). Một ván chơi kiếm khoảng vài chục tới vài trăm xu (ước tính của tôi, chưa đo trên người chơi thật), nên mất hàng trăm ván cho một bộ. Với mục tiêu "đủ bộ mở kỹ năng" thì quá nặng.

### Đề xuất: Mảnh sao (universal shards)
- Mỗi lần quay **trùng**, ngoài xu hoàn lại, nhận thêm **1 mảnh sao**.
- **20 mảnh sao** đổi lấy một chú mèo **tuỳ chọn** còn thiếu của bất kỳ chủ đề nào (chỉ mèo, không phải nền/khung).
- Kết quả mô phỏng (cũng cho một chủ đề):

| Chi phí ghép | Lượt quay trung bình | Trung vị | 90% người chơi cần tối đa |
|---|---|---|---|
| 10 mảnh | 101 | 101 | 120 |
| **20 mảnh** (đề xuất) | **134** | 129 | 166 |
| 30 mảnh | 153 | 158 | 193 |
| 50 mảnh | 184 | 175 | 229 |

Với 20 mảnh: từ 464 xuống **khoảng 134 lượt** (≈ 20.000 xu), còn lại là chuyện cân đối thu nhập.
Điều này cũng giải quyết cảm giác bực khi quay mãi vẫn thiếu đúng con cuối.

### Các đòn bẩy khác (có thể dùng kèm hoặc thay thế)
- Giảm giá một lượt (150 → 100 xu).
- Tăng xu từ gộp mèo, hoặc thêm phần thưởng hằng ngày / nhiệm vụ (đã nằm trong danh sách ý tưởng cũ).
- Tăng tỉ lệ Huyền thoại, vì chính các con cấp 10–11 (Huyền thoại) là điểm nghẽn.
- Bảo hiểm theo chủ đề: lượt thứ N chắc chắn ra một con còn thiếu của chủ đề bạn đang theo dõi.

> Ghi chú: mô phỏng này giả định mọi món cùng độ hiếm có xác suất ngang nhau, giống code hiện tại. Nếu đổi bảng xác suất thì cần chạy lại.

---

## 7. Giao diện

### 7.1 Trong game
```
┌──────────────────────────────┐
│ 1520              ⚙      NEXT│
│ BEST 3606               (🐱) │
│ (🪙 1891)               ╭───╮│
│                         │ ✨││  ← nút kỹ năng (vòng năng lượng quanh viền)
│                         ╰───╯│
│   ┌────────────────────┐     │
│   │                    │     │
│   │     khung chơi     │     │
```
- Đang chọn mục tiêu: nền tối, dòng chữ "Chạm một chú mèo để dùng kỹ năng", nút `HUỶ`.
- Đang chạy kỹ năng theo thời gian: đồng hồ đếm ngược quanh nút, hiệu ứng riêng của chủ đề.
- Vị trí cuối cùng còn cần thử trên màn hình thật để không che mèo đang thả.

### 7.2 Skin tab
- Ở dòng chọn chủ đề thêm khung **"Kỹ năng bộ"**: tên + trạng thái (khoá / mở khoá / đang dùng), bấm vào xem mô tả.
- Nút **MẶC CẢ BỘ NÀY** có tiến độ "9/11 mèo trong bộ" và, khi đã đủ bộ, hiện thêm "để dùng kỹ năng".

### 7.3 Sổ mèo
- Mỗi trang chủ đề có thêm **thẻ kỹ năng** ở cuối trang: biểu tượng, tên, mô tả ngắn, và "Gom đủ 11 con để mở khoá" kèm thanh tiến độ.
- Cho phép **xem trước kỹ năng của bộ chưa có** (chỉ đọc mô tả) để tạo động lực.

### 7.4 Gacha
- Khi một lượt quay đưa một bộ lên đủ 11/11: màn "HOÀN THÀNH BỘ" trước khi về lại.
- Thẻ kết quả của một con mèo thuộc bộ chưa đủ thêm dòng nhỏ: "Bộ Pixel 8/11".

---

## 8. Thay đổi kỹ thuật dự kiến

| Phần | Việc cần làm |
|---|---|
| `Cosmetics` (Data) | Thêm định nghĩa kỹ năng cho từng chủ đề (id, tên EN/VI, mô tả EN/VI, loại, thông số). Hàm `ActiveSkill` (đủ bộ + mặc đủ 11 vị trí), `IsSetComplete(theme)`, `OwnedInTheme(theme)` (đã có). |
| `PlayerData` | Cờ "đã xem thông báo mở khoá bộ" để không hiện lại; số mảnh sao nếu làm mục 6. |
| `RunSnapshot` | Thêm `energy` (năng lượng hiện tại) để tiếp tục ván dở. |
| `Balance` + `balance.json` | Mục `skills`: năng lượng mỗi lần gộp, thời lượng, hệ số, cấp tối đa, giá Midas, chi phí mảnh sao... |
| `GameManager` | Hook vào sự kiện gộp để nạp năng lượng; hỗ trợ tạm dừng bộ đếm thua, nhân điểm/xu, xoá/đổi cấp một mèo, lực hút giữa các mèo, thu nhỏ bán kính. |
| Mới: `SkillSystem` (Gameplay) | Quản lý năng lượng, trạng thái chọn mục tiêu, hiệu ứng đang chạy. Mỗi kỹ năng là một lớp nhỏ để dễ thêm sau. |
| `GameUI` | Nút kỹ năng, chế độ chọn mục tiêu, đồng hồ đếm ngược. |
| `SkinPage` / `CodexScreen` / `GachaPage` | Các thẻ và nhãn ở mục 7. |
| `Loc` | Khoảng 7 × (tên + mô tả) + vài chuỗi giao diện, cả EN và VI. |
| `AudioManager` / `Fx` | Âm thanh và hiệu ứng riêng cho từng kỹ năng (vẫn tạo bằng code, không cần file). |
| Cheat | Thêm "mở khoá mọi kỹ năng" và "nạp đầy năng lượng" để thử nhanh. |

### Thứ tự triển khai đề xuất
1. **Khung:** luật mở khoá/kích hoạt, hiển thị trên Skin/Sổ mèo, nút + thanh năng lượng, lưu năng lượng, cheat. Làm kèm 2 kỹ năng đơn giản nhất: **Cơn sốt đường** và **Ánh trăng**.
2. **Kỹ năng chọn mục tiêu:** Tẩy, Lên cấp, Midas.
3. **Kỹ năng vật lý:** Cái ôm, Thu nhỏ (hoặc Nảy lên). Cần thử nhiều nhất nên làm sau.
4. **Kinh tế:** mảnh sao, màn "Hoàn thành bộ", chỉnh cân bằng.
5. **Hoàn thiện:** hiệu ứng, âm thanh, thử trên điện thoại thật.

---

## 9. Rủi ro tổng hợp

| Rủi ro | Mức | Cách giảm |
|---|---|---|
| Đủ bộ quá khó, người chơi bỏ cuộc | **Cao** | Mảnh sao (mục 6), chỉnh giá/tỉ lệ |
| Kỹ năng vật lý gây lỗi (rung, mèo văng ra ngoài) | Trung bình | Làm sau cùng, lực nhỏ, thử trên điện thoại yếu |
| Kỹ năng quá mạnh làm ván quá dễ | Trung bình | Đo điểm trung bình, chỉnh trong `balance.json` |
| Kỹ năng tăng xu làm lạm phát | Trung bình | Chỉ nhân xu gộp, không nhân thưởng khác |
| Người chơi không hiểu vì sao không có kỹ năng | Trung bình | Nhãn tiến độ rõ ở Skin, nút tắt tới chỗ còn thiếu |
| Làm phức tạp giao diện chơi | Thấp | Một nút duy nhất, ẩn khi không có kỹ năng |

---

## 10. Cần bạn quyết định

1. **Cách dùng:** kỹ năng nạp bằng gộp mèo (đề xuất) hay mỗi ván có số lần dùng cố định?
2. **Mức nạp:** +6 mỗi lần gộp, bắt đầu 30%, tốn 100 có ổn không? Muốn dùng nhiều hay ít hơn?
3. **Mặc chưa đủ bộ:** có muốn bản **yếu hơn** khi mặc từ 6/11 trở lên (ví dụ thời gian ngắn hơn), hay giữ đúng "đủ 11 mới dùng"?
4. **Kinh tế:** có làm **mảnh sao** (20 mảnh đổi một con) không? Hay chỉ chỉnh giá/tỉ lệ?
5. **Chibi:** giữ **Thu nhỏ** (hay nhưng có rủi ro vật lý) hay chọn **Nảy lên** (ổn định hơn)?
6. **Kẹo ngọt:** nhân xu ×1,5 có chấp nhận được không, hay chỉ nhân điểm?
7. **Pixel:** giới hạn nâng cấp tối đa cấp 8 có hợp lý không?
8. **Xem trước kỹ năng chưa mở khoá** trong Sổ mèo: có (đề xuất) hay giữ bí ẩn?
9. **Tiếp tục ván dở:** lưu năng lượng (đề xuất), hay đặt lại về 30%?
10. **Tên kỹ năng** ở mục 4 đã ổn chưa, bạn có tên khác muốn dùng?

Khi bạn chốt các mục này, tôi bắt đầu từ bước 1 của mục 8.
