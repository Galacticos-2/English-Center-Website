import TeacherSidebar from "../../lesson/teacher/TeacherSidebar";
import OnlineClassroomContent from "../components/OnlineClassroomContent";

function TeacherOnlineClassroomPage() {
    return <div className="lesson-layout"><TeacherSidebar /><OnlineClassroomContent isTeacher /></div>;
}

export default TeacherOnlineClassroomPage;
