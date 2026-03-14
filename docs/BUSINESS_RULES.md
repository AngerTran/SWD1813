# Business Rules (BR) – Đầy đủ

Tài liệu mô tả toàn bộ **29 Business Rules** của hệ thống quản lý dự án (Jira + GitHub integration). Mỗi rule gồm: ID, mô tả, phạm vi, cách kiểm tra và nơi áp dụng trong code.

---

## 5.1 User Role Rules (Quy tắc vai trò người dùng)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR1** | Mỗi user có một role duy nhất | Một tài khoản người dùng chỉ được gán đúng một vai trò trong hệ thống: Admin, Lecturer, Leader hoặc Member. Không cho phép một user đồng thời mang nhiều role. |
| **BR2** | Chỉ Admin được tạo nhóm | Chức năng tạo nhóm sinh viên (Create group) chỉ được thực hiện bởi user có role **Admin**. Lecturer, Team Leader, Team Member không có quyền tạo nhóm. |
| **BR3** | Chỉ Admin gán lecturer cho nhóm | Việc gán giảng viên (Lecturer) phụ trách một nhóm chỉ được thực hiện bởi user có role **Admin**. |
| **BR4** | Mỗi nhóm phải có Team Leader | Mỗi nhóm sinh viên phải có ít nhất một thành viên với vai trò **Leader** (Team Leader). Hệ thống không cho phép nhóm tồn tại mà không có Leader; khi xóa hoặc chuyển Leader cuối cùng phải chỉ định Leader mới hoặc từ chối thao tác. |
| **BR5** | Một sinh viên chỉ thuộc một nhóm | Một user có role Member/Leader (sinh viên) chỉ được phép thuộc về đúng một nhóm tại một thời điểm. Không cho phép thêm sinh viên vào nhóm thứ hai nếu đã ở trong một nhóm. |

**Phạm vi áp dụng:** Bảng `Users`, `Groups`, `GroupMembers`; logic tạo/cập nhật nhóm, thêm/xóa member, gán lecturer.  
**Kiểm tra:** Khi tạo group → kiểm tra role = Admin. Khi gán lecturer → kiểm tra role = Admin. Khi thêm member → kiểm tra user chưa thuộc nhóm nào khác. Khi remove Leader → đảm bảo nhóm còn ít nhất một Leader.

---

## 5.2 Project Rules (Quy tắc dự án)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR6** | Mỗi nhóm phải liên kết với một Jira Project | Mỗi nhóm (Group) phải được liên kết với đúng một Jira Project (thông qua project key, ví dụ SWP). Một nhóm không thể thiếu liên kết Jira khi vận hành đầy đủ tính năng (sync task, requirement). |
| **BR7** | Mỗi nhóm phải liên kết với một GitHub Repository | Mỗi nhóm phải được liên kết với đúng một GitHub Repository. Repository này dùng để đồng bộ commit và đánh giá đóng góp thành viên. |
| **BR8** | Jira project phải được cấu hình trước khi sync | Trước khi thực hiện đồng bộ (sync) dữ liệu từ Jira (issues, backlog, sprint), Jira project phải đã được kết nối và cấu hình (base URL, API token, project key hợp lệ). Nếu chưa cấu hình thì không cho phép thao tác sync. |
| **BR9** | GitHub repository phải có quyền API | Repository GitHub được liên kết phải có quyền truy cập qua API (token có quyền đọc repo, commits). Nếu API trả về 403/404 hoặc không có quyền thì không coi là cấu hình hợp lệ và cần thông báo người dùng. |

**Phạm vi áp dụng:** Bảng `Groups`, `Projects`, `Repositories`; service Jira/GitHub, màn hình kết nối project/repo.  
**Kiểm tra:** Khi sync Jira → kiểm tra project đã có `jira_project_key` và kết nối thành công. Khi sync GitHub → gọi API thử và kiểm tra quyền đọc.

---

## 5.3 Requirement Rules (Quy tắc requirement)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR10** | Requirement phải tồn tại dưới dạng Jira Issue | Mọi requirement trong hệ thống phải bắt nguồn từ một Jira Issue (được import/sync từ Jira). Không tạo requirement “thủ công” ngoài Jira. |
| **BR11** | Requirement phải có Title và Description | Requirement (Jira Issue dùng làm requirement) phải có trường Title (summary) và Description không rỗng. Issue thiếu một trong hai không được coi là requirement hợp lệ khi xuất SRS hoặc báo cáo. |
| **BR12** | Requirement phải có Priority | Mỗi requirement phải có thông tin Priority (từ Jira: Highest, High, Medium, Low, Lowest hoặc tương đương). Thiếu priority có thể mặc định hoặc cảnh báo tùy quyết định nghiệp vụ. |
| **BR13** | Requirement phải có Status | Mỗi requirement phải có trạng thái (Status) từ Jira (ví dụ To Do, In Progress, Done). Dùng để theo dõi tiến độ và lọc trong báo cáo. |

**Phạm vi áp dụng:** Bảng `JiraIssues`; logic import từ Jira, SRS generation, requirement listing.  
**Kiểm tra:** Khi import/sync: map đủ summary, description, priority, status. Khi xuất SRS: chỉ lấy issue thỏa BR11–BR13 hoặc đánh dấu thiếu thông tin.

---

## 5.4 Task Assignment Rules (Quy tắc phân công task)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR14** | Chỉ Team Leader assign task | Chỉ user có vai trò **Team Leader** (Leader trong nhóm) được phép gán task cho thành viên. Admin/Lecturer/Member không có quyền assign task (trừ khi mở rộng nghiệp vụ sau). |
| **BR15** | Mỗi task có một người phụ trách | Mỗi task tại một thời điểm chỉ có tối đa một người được gán (assignee). Một task không thể được gán cho nhiều người đồng thời. |
| **BR16** | Một user có thể có nhiều task | Một thành viên (user) có thể được gán nhiều task cùng lúc. Hệ thống không giới hạn số task trên một user. |
| **BR17** | Task phải có deadline | Mỗi task phải có thông tin deadline (ngày hạn hoàn thành). Task không có deadline phải được cảnh báo hoặc bắt buộc nhập trước khi lưu/cập nhật. |

**Phạm vi áp dụng:** Bảng `Tasks`, `GroupMembers`; service assign task, form tạo/cập nhật task.  
**Kiểm tra:** Khi assign → kiểm tra người thực hiện là Team Leader; khi lưu task → kiểm tra `assigned_to` tối đa một user và `deadline` có giá trị.

---

## 5.5 Commit Tracking Rules (Quy tắc theo dõi commit)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR18** | Commit phải thuộc repository của nhóm | Chỉ những commit thuộc repository GitHub đã liên kết với nhóm mới được tính vào thống kê và đánh giá đóng góp của nhóm đó. Commit từ repo khác không được gắn với nhóm. |
| **BR19** | Commit phải liên kết GitHub account | Để đánh giá đóng góp theo thành viên, commit cần được liên kết với tài khoản/identity (ví dụ email hoặc GitHub username) để map với User/GroupMember trong hệ thống. Commit không map được có thể bỏ qua hoặc hiển thị “Unknown”. |
| **BR20** | Commit phải trong thời gian project | Chỉ commit có thời gian (commit_date) nằm trong khoảng thời gian của project (start_date → end_date) mới được tính vào báo cáo và đánh giá. Commit ngoài khoảng thời gian có thể ẩn hoặc loại trừ. |
| **BR21** | Merge commit không tính đóng góp | Merge commit (commit có nhiều parent hoặc message chứa “Merge” theo quy ước) không được tính vào đóng góp của thành viên (để tránh làm méo số liệu khi merge branch). |

**Phạm vi áp dụng:** Bảng `Commits`, `Repositories`, `Projects`; service import commit từ GitHub, tính ContributorStats.  
**Kiểm tra:** Khi import: chỉ lấy commit từ repo của nhóm; lọc theo `commit_date` trong [start_date, end_date]; bỏ qua merge commit khi cộng đóng góp.

---

## 5.6 Contribution Evaluation Rules (Quy tắc đánh giá đóng góp)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR22** | Đóng góp dựa trên commit và task | Việc đánh giá đóng góp của thành viên dựa trên hai nguồn: (1) số commit và thay đổi code (additions/deletions) từ GitHub, (2) số task được gán và hoàn thành. Không dùng nguồn khác ngoài hai nguồn này cho đánh giá chuẩn. |
| **BR23** | Task chỉ hoàn thành khi trạng thái Done | Task chỉ được coi là “hoàn thành” khi trạng thái (status) là **Done** (hoặc tương đương theo Jira). Các trạng thái khác (To Do, In Progress, …) không tính là hoàn thành khi tính % tiến độ hoặc contribution. |
| **BR24** | Commit nên liên quan Jira issue | Khuyến khích commit message có tham chiếu đến Jira issue (ví dụ “SWP-123 Fix login”). Hệ thống có thể dùng để hiển thị “commit liên quan issue” hoặc cảnh báo commit không liên kết; không bắt buộc từ chối. |
| **BR25** | Không có commit sẽ bị đánh dấu low contribution | Thành viên không có bất kỳ commit nào trong khoảng thời gian đánh giá (sprint/project) sẽ được đánh dấu là **low contribution** (đóng góp thấp) trên dashboard hoặc báo cáo. |

**Phạm vi áp dụng:** Logic tính ContributorStats, dashboard, báo cáo tiến độ và đóng góp.  
**Kiểm tra:** Khi tính % task hoàn thành → chỉ đếm task status = Done. Khi hiển thị contribution → kết hợp commit + task done; nếu total_commits = 0 → hiển thị low contribution.

---

## 5.7 Reporting Rules (Quy tắc báo cáo)

| ID | Rule | Mô tả đầy đủ |
|----|------|----------------|
| **BR26** | Báo cáo tạo theo sprint hoặc tuần | Báo cáo (progress, commit) được tạo theo đơn vị thời gian: theo **sprint** (nếu dùng Jira sprint) hoặc theo **tuần**. Người dùng chọn sprint hoặc khoảng tuần khi tạo báo cáo. |
| **BR27** | Commit report hiển thị theo member | Báo cáo commit phải hiển thị dữ liệu theo từng thành viên (member): số commit, lines added/deleted, có thể kèm danh sách commit. |
| **BR28** | Progress report hiển thị % task | Báo cáo tiến độ (progress report) phải hiển thị phần trăm task hoàn thành (ví dụ 70% Done) cho project/sprint, có thể kèm theo số task Done / tổng task. |
| **BR29** | Báo cáo có thể export PDF | Hệ thống phải hỗ trợ xuất báo cáo ra định dạng PDF. Các loại báo cáo (progress, commit, SRS) đều có thể export PDF theo yêu cầu. |

**Phạm vi áp dụng:** Module report, export (PDF/DOCX), dashboard.  
**Kiểm tra:** Khi tạo report → có tham số sprint hoặc tuần; commit report có cột/theo member; progress report có % task; nút/action export PDF khả dụng.

---

## Tổng hợp BR theo nhóm

| Nhóm | BR | Số lượng |
|------|-----|----------|
| User Role Rules | BR1 – BR5 | 5 |
| Project Rules | BR6 – BR9 | 4 |
| Requirement Rules | BR10 – BR13 | 4 |
| Task Assignment Rules | BR14 – BR17 | 4 |
| Commit Tracking Rules | BR18 – BR21 | 4 |
| Contribution Evaluation Rules | BR22 – BR25 | 4 |
| Reporting Rules | BR26 – BR29 | 4 |
| **Tổng** | **BR1 – BR29** | **29** |

---

## Ánh xạ BR → Nơi áp dụng trong code (gợi ý)

| BR | Gợi ý nơi áp dụng |
|----|-------------------|
| BR1 | Entity User (role duy nhất); seed/update user |
| BR2 | GroupsController Create – Authorize(Roles = "Admin") |
| BR3 | GroupsController AssignLecturer – Authorize(Roles = "Admin") |
| BR4 | GroupService: khi remove leader kiểm tra còn leader; khi tạo nhóm bắt buộc có ít nhất 1 leader |
| BR5 | GroupService AddMember: kiểm tra user chưa thuộc nhóm khác |
| BR6, BR7 | Project/Group entity và UI: bắt buộc chọn Jira project + GitHub repo khi kích hoạt đầy đủ |
| BR8 | JiraSyncService: kiểm tra project đã cấu hình trước khi gọi API sync |
| BR9 | GitHubSyncService: kiểm tra quyền API khi connect/sync |
| BR10 | Chỉ hiển thị requirement từ JiraIssue, không tạo requirement thủ công |
| BR11–BR13 | JiraIssue validation; SrsGenerationService filter issue có summary, description, priority, status |
| BR14 | TasksController Assign – chỉ Team Leader (role Leader trong nhóm) |
| BR15 | Task entity: một assigned_to; validation khi assign |
| BR16 | Không giới hạn số task trên user |
| BR17 | Task entity: deadline required; validation khi tạo/cập nhật task |
| BR18 | Commit import: chỉ repo của nhóm; filter repo_id |
| BR19 | Map author_email/author với User; ContributorStats theo user_id |
| BR20 | Commit filter: commit_date BETWEEN project.start_date AND project.end_date |
| BR21 | Commit import: bỏ qua commit có is_merge hoặc message chứa "Merge" |
| BR22 | Dashboard/ContributorStats: chỉ dùng commit + task done |
| BR23 | Task completion: status = "Done" mới đếm |
| BR24 | Optional: parse commit message tìm issue key; hiển thị “linked issue” |
| BR25 | Dashboard: total_commits = 0 → badge/low contribution |
| BR26 | ReportService: tham số SprintId hoặc StartWeek/EndWeek |
| BR27 | Commit report: group by member, hiển thị theo member |
| BR28 | Progress report: (Done count / Total) * 100 |
| BR29 | ReportExportService: export PDF cho mọi loại báo cáo |

File này dùng làm tài liệu tham chiếu khi triển khai và test từng tính năng.
