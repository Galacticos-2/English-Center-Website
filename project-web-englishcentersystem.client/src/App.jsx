import {
    BrowserRouter,
    Routes,
    Route
} from "react-router-dom";

import LoginPage
from "./features/auth/pages/LoginPage";
import TeacherTestResultPage
    from "./features/test/teacher/pages/TeacherTestResultPage";
import StudentTestHistoryPage
    from "./features/test/student/pages/StudentTestHistoryPage";
import ProtectedRoute
    from "./features/auth/components/ProtectedRoute";
import TeacherStudentDashboardPage
    from "./features/teacher/pages/TeacherStudentDashboardPage";
import MainLayout from "./features/layout/MainLayout";
import MyInfoPage from "./features/profile/pages/MyInfoPage";
import RoleRoute
from "./features/auth/components/RoleRoute";
import TeacherTestPage
    from "./features/test/teacher/pages/TeacherTestPage";
import TeacherReadingPage
    from "./features/reading/teacher/pages/TeacherReadingPage";
import AdminPage
    from "./features/admin/pages/AdminPage";
import StudentExerciseResultPage
    from "./features/student/pages/StudentExerciseResultPage";
import StudentLessonPage
    from "./features/lesson/student/StudentLessonPage";
import TeacherPage
    from "./features/admin/pages/TeacherPage";
import NotificationCenterPage
    from "./features/notification/pages/NotificationCenterPage";

import StudentPage
from "./features/student/pages/StudentPage";
import StudentClassLeaderboardPage
from "./features/student/pages/StudentClassLeaderboardPage";
import { useAuthBootstrap }
    from "./features/auth/hooks/useAuthBootstrap";
import StudentTestResultPage
    from "./features/test/student/pages/StudentTestResultPage";
import HomePage
    from "./features/home/pages/HomePage";
import CoursePage
    from "./features/admin/pages/CoursePage";
import AdminDashboardPage
    from "./features/admin/pages/AdminDashboardPage";
import StudentExerciseHistoryPage
    from "./features/student/pages/StudentExerciseHistoryPage";
import StudentTestPage
    from "./features/test/student/pages/StudentTestPage";
import TeacherCompetitionPage
    from "./features/competition/teacher/pages/TeacherCompetitionPage";

import TeacherCompetitionLeaderboardPage
    from "./features/competition/teacher/pages/TeacherCompetitionLeaderboardPage";
import TeacherExerciseResultPage
    from "./features/exercise/teacher/pages/TeacherExerciseResultPage";
import TeacherExercisePage
    from "./features/exercise/teacher/pages/TeacherExercisePage";
import ExercisePage
from "./features/student/pages/ExercisePage";
import TeacherSpeakingImportPage
from "./features/speaking/teacher/TeacherSpeakingImportPage";
import PracticeTestPage
from "./features/student/pages/PracticeTestPage";
import TeacherLessonPage
    from "./features/lesson/teacher/TeacherLessonPage";
import MockTestPage
    from "./features/student/pages/MockTestPage";
import StudentCompetitionPage
    from "./features/competition/student/pages/StudentCompetitionPage";

import StudentCompetitionAttemptPage
    from "./features/competition/student/pages/StudentCompetitionAttemptPage";

import StudentCompetitionResultPage
    from "./features/competition/student/pages/StudentCompetitionResultPage";

import StudentCompetitionLeaderboardPage
    from "./features/competition/student/pages/StudentCompetitionLeaderboardPage";
import TeacherHomePage
    from "./features/teacher/pages/TeacherHomePage";
import TeacherOnlineClassroomPage
    from "./features/online-classroom/pages/TeacherOnlineClassroomPage";
import StudentOnlineClassroomPage
    from "./features/online-classroom/pages/StudentOnlineClassroomPage";
import TeacherFlashcardPage
    from "./features/flashcard/pages/TeacherFlashcardPage";
import StudentFlashcardPage
    from "./features/flashcard/pages/StudentFlashcardPage";
function App() {
    useAuthBootstrap();
    return (
        
        <BrowserRouter>

            <Routes>

                <Route
                    path="/"
                    element={<HomePage />}
                />

                <Route
                    path="/login"
                    element={<LoginPage />}
                />
                <Route element={<MainLayout />}>
                    <Route
                        path="/my-info"
                        element={
                            <ProtectedRoute>
                                <MyInfoPage />
                            </ProtectedRoute>
                        }
                    />
                   
                    <Route
                        path="/teacher/exercises"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={[
                                        "Teacher"
                                    ]}
                                >
                                    <TeacherExercisePage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
    path="/teacher/exams"
    element={
        <ProtectedRoute>
            <RoleRoute allowedRoles={["Teacher"]}>
                <TeacherTestPage />
            </RoleRoute>
        </ProtectedRoute>
    }
/>
                    <Route
    path="/teacher/speaking"
    element={
        <ProtectedRoute>
            <RoleRoute
                allowedRoles={[
                    "Teacher"
                ]}
            >
                <TeacherSpeakingImportPage/>
            </RoleRoute>
        </ProtectedRoute>
    }
/>
                   
                <Route
                    path="/admin/courses"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Admin"
                                ]}
                            >
                                <CoursePage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/student/learning"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Student"
                                ]}
                            >
                                <StudentLessonPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                    />

                <Route
                    path="/admin/teachers"
                    element={
                        <TeacherPage />
                    }
                />
                <Route
    path="/student/tests"
    element={
        <ProtectedRoute>
            <RoleRoute allowedRoles={["Student"]}>
                <StudentTestPage />
            </RoleRoute>
        </ProtectedRoute>
    }
/>
                <Route
                    path="/teacher/lessons"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Teacher"
                                ]}
                            >
                                <TeacherLessonPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                />
                <Route
    path="/teacher/test-results"
    element={
        <ProtectedRoute>
            <RoleRoute allowedRoles={["Teacher"]}>
                <TeacherTestResultPage />
            </RoleRoute>
        </ProtectedRoute>
    }
/>
                <Route
                    path="/admin/students"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Admin"
                                ]}
                            >
                                <AdminPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                    />
                  
                    <Route
                        path="/student/tests/:testAssignmentId/history"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Student"]}>
                                    <StudentTestHistoryPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/competitions"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Student"]}
                                >
                                    <StudentCompetitionPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />

                    <Route
                        path="/student/competitions/:competitionTestAssignmentId/attempt"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Student"]}
                                >
                                    <StudentCompetitionAttemptPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/teacher/students"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={[
                                        "Teacher"
                                    ]}
                                >
                                    <TeacherStudentDashboardPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/competitions/result"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Student"]}
                                >
                                    <StudentCompetitionResultPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />

                    <Route
                        path="/student/competitions/:competitionTestAssignmentId/leaderboard"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Student"]}
                                >
                                    <StudentCompetitionLeaderboardPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/notifications"
                        element={
                            <ProtectedRoute>
                                <NotificationCenterPage />
                            </ProtectedRoute>
                        }
                    />
                <Route
                    path="/admin/dashboard"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Admin"
                                ]}
                            >
                                <AdminDashboardPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                    />
                    <Route
                        path="/student/exercise-history"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={[
                                        "Student"
                                    ]}
                                >
                                    <StudentExerciseHistoryPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/teacher/competition"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Teacher"]}
                                >
                                    <TeacherCompetitionPage />
                                </RoleRoute>
                            </ProtectedRoute>
                    }
                    />
                    <Route
                        path="/teacher/flashcards"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Teacher"]}>
                                    <TeacherFlashcardPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/teacher/online-classrooms"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Teacher"]}>
                                    <TeacherOnlineClassroomPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/online-classrooms"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Student"]}>
                                    <StudentOnlineClassroomPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/flashcards"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Student"]}>
                                    <StudentFlashcardPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/exercises/result"
                        element={<StudentExerciseResultPage />}
                    />
                    <Route
                        path="/teacher/competitions/:competitionTestAssignmentId/leaderboard"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={["Teacher"]}
                                >
                                    <TeacherCompetitionLeaderboardPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/teacher/exercise-results"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={[
                                        "Teacher"
                                    ]}
                                >
                                    <TeacherExerciseResultPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/teacher/reading"
                        element={
                            <ProtectedRoute>
                                <RoleRoute
                                    allowedRoles={[
                                        "Teacher"
                                    ]}
                                >
                                    <TeacherReadingPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/student/tests/result"
                        element={
                            <ProtectedRoute>
                                <RoleRoute allowedRoles={["Student"]}>
                                    <StudentTestResultPage />
                                </RoleRoute>
                            </ProtectedRoute>
                        }
                    />
                <Route
                    path="/teacher"
                    element={
                        <ProtectedRoute>
                            <RoleRoute allowedRoles={["Teacher"]}>
                                <TeacherHomePage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                />
                
                <Route
                    path="/student"
                    element={
                        <ProtectedRoute>
                            <RoleRoute
                                allowedRoles={[
                                    "Student"
                                ]}
                            >
                                <StudentPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/student/class-leaderboard"
                    element={
                        <ProtectedRoute>
                            <RoleRoute allowedRoles={["Student"]}>
                                <StudentClassLeaderboardPage />
                            </RoleRoute>
                        </ProtectedRoute>
                    }
                />
                <Route
    path="/student/learning"
    element={
        <ProtectedRoute>
            <RoleRoute
                allowedRoles={[
                    "Student"
                ]}
            >
                
            </RoleRoute>
        </ProtectedRoute>
    }
/>
            <Route
    path="/student/exercises"
    element={
        <ProtectedRoute>
            <RoleRoute
                allowedRoles={[
                    "Student"
                ]}
            >
                <ExercisePage />
            </RoleRoute>
        </ProtectedRoute>
    }
/>
            <Route
    path="/student/practice-tests"
    element={
        <ProtectedRoute>
            <RoleRoute
                allowedRoles={[
                    "Student"
                ]}
            >
                <PracticeTestPage />
            </RoleRoute>
        </ProtectedRoute>
    }
/>
            <Route
    path="/student/mock-tests"
    element={
        <ProtectedRoute>
            <RoleRoute
                allowedRoles={[
                    "Student"
                ]}
            >
                <MockTestPage />
            </RoleRoute>
        </ProtectedRoute>
    }
                    />
                </Route>
            </Routes>
            

        </BrowserRouter>
    );
}

export default App;
