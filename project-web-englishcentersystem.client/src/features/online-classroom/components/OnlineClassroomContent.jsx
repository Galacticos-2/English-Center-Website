import { useEffect, useState } from "react";
import { Space, Table, Tag, Tooltip } from "antd";
import { CalendarDays, ChevronRight, ExternalLink, Pencil, Plus, Trash2, Video, X } from "lucide-react";
import { getMyTeacherClassesApi } from "../../lesson/api/lessonApi";
import {
    createOnlineClassroomApi,
    deleteOnlineClassroomApi,
    getStudentOnlineClassroomsApi,
    getTeacherOnlineClassroomsApi,
    updateOnlineClassroomApi
} from "../api/onlineClassroomApi";
import "../styles/onlineClassroom.css";

const emptyForm = {
    classRoomId: "",
    title: "",
    googleMeetUrl: "",
    startsAt: "",
    endsAt: ""
};

const parseOnlineClassroomDate = (value) => {
    if (!value) return null;
    if (value instanceof Date) return value;

    const normalizedValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value)
        ? value
        : `${value}Z`;
    const date = new Date(normalizedValue);
    return Number.isNaN(date.getTime()) ? null : date;
};

const toInputDateTime = (value) => {
    const date = parseOnlineClassroomDate(value);
    if (!date) return "";
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
};

const getNewClassroomForm = () => {
    const startsAt = new Date();
    const endsAt = new Date(startsAt.getTime() + 60 * 60 * 1000);

    return {
        ...emptyForm,
        startsAt: toInputDateTime(startsAt),
        endsAt: toInputDateTime(endsAt)
    };
};

const getStatus = (room) => {
    const now = Date.now();
    if (now < parseOnlineClassroomDate(room.startsAt).getTime()) return ["Scheduled", "scheduled"];
    if (now > parseOnlineClassroomDate(room.endsAt).getTime()) return ["Ended", "ended"];
    return ["Live now", "live"];
};

const formatDateTime = (value) => new Intl.DateTimeFormat("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Ho_Chi_Minh"
}).format(parseOnlineClassroomDate(value));

function OnlineClassroomContent({ isTeacher }) {
    const [rooms, setRooms] = useState([]);
    const [classes, setClasses] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [showForm, setShowForm] = useState(false);
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");

    const load = async () => {
        setLoading(true);
        setError("");
        try {
            const [roomData, classData] = await Promise.all([
                isTeacher ? getTeacherOnlineClassroomsApi() : getStudentOnlineClassroomsApi(),
                isTeacher ? getMyTeacherClassesApi() : Promise.resolve([])
            ]);
            setRooms(roomData || []);
            setClasses(classData || []);
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to load online classrooms.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, [isTeacher]);

    const closeForm = () => {
        setForm(emptyForm);
        setEditingId(null);
        setShowForm(false);
    };

    const submit = async (event) => {
        event.preventDefault();
        if (new Date(form.endsAt) <= new Date(form.startsAt)) {
            setError("End time must be after the start time.");
            return;
        }

        setSubmitting(true);
        setError("");
        const payload = {
            ...form,
            classRoomId: Number(form.classRoomId),
            startsAt: new Date(form.startsAt).toISOString(),
            endsAt: new Date(form.endsAt).toISOString()
        };
        try {
            if (editingId) await updateOnlineClassroomApi(editingId, payload);
            else await createOnlineClassroomApi(payload);
            closeForm();
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to save the online classroom.");
        } finally {
            setSubmitting(false);
        }
    };

    const edit = (room) => {
        setEditingId(room.onlineClassroomId);
        setForm({
            classRoomId: String(room.classRoomId),
            title: room.title,
            googleMeetUrl: room.googleMeetUrl,
            startsAt: toInputDateTime(room.startsAt),
            endsAt: toInputDateTime(room.endsAt)
        });
        setShowForm(true);
    };

    const openCreateForm = () => {
        setError("");
        setForm(getNewClassroomForm());
        setEditingId(null);
        setShowForm(true);
    };

    const updateStartTime = (startsAt) => {
        setForm((currentForm) => {
            const nextForm = { ...currentForm, startsAt };
            if (!currentForm.endsAt || new Date(currentForm.endsAt) <= new Date(startsAt)) {
                nextForm.endsAt = toInputDateTime(new Date(new Date(startsAt).getTime() + 60 * 60 * 1000));
            }
            return nextForm;
        });
    };

    const remove = async (id) => {
        if (!window.confirm("Delete this online classroom?")) return;
        try {
            await deleteOnlineClassroomApi(id);
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to delete the online classroom.");
        }
    };

    return (
        <section className={`online-classroom-page${isTeacher ? " teacher-online-classroom" : ""}`}>
            {isTeacher && (
                <div className="online-classroom-breadcrumb">
                    <span>Home</span>
                    <ChevronRight size={16} />
                    <strong>Online Classroom</strong>
                </div>
            )}

            <header className="online-classroom-heading">
                <div className="online-classroom-title">
                    {isTeacher && <div className="online-classroom-title-icon"><Video size={20} /></div>}
                    <div>
                        {isTeacher && <span className="online-classroom-eyebrow">LIVE LEARNING</span>}
                        <h1>Online Classroom</h1>
                    <p>{isTeacher ? "Create Google Meet sessions for your assigned classes." : "Join Google Meet sessions created for your class."}</p>
                    </div>
                </div>
                {isTeacher && (
                    <button className="online-classroom-primary" type="button" onClick={openCreateForm}>
                        <Plus size={18} /> Create classroom
                    </button>
                )}
            </header>

            {error && <div className="online-classroom-error">{error}</div>}

            <div className="online-classroom-content">
                {loading ? (
                    <div className="online-classroom-empty">Loading classrooms...</div>
                ) : rooms.length === 0 ? (
                    <div className="online-classroom-empty"><Video size={34} /><h2>No online classrooms yet</h2><p>{isTeacher ? "Create a Google Meet session for one of your classes." : "Your teacher has not created an online classroom for your class yet."}</p></div>
                ) : isTeacher ? (
                    <Table
                        className="online-classroom-table"
                        dataSource={rooms}
                        rowKey="onlineClassroomId"
                        tableLayout="fixed"
                        pagination={{
                            pageSize: 10,
                            showSizeChanger: true,
                            showTotal: total => `${total} classrooms`
                        }}
                        columns={[
                            {
                                title: "Classroom",
                                key: "title",
                                width: 210,
                                render: (_, room) => (
                                    <div className="online-classroom-table-title">
                                        <Tooltip title={room.title}>
                                            <strong>{room.title}</strong>
                                        </Tooltip>
                                    </div>
                                )
                            },
                            {
                                title: "Class",
                                key: "class",
                                render: (_, room) => `${room.className}${room.courseName ? ` · ${room.courseName}` : ""}`
                            },
                            {
                                title: "Status",
                                key: "status",
                                width: 105,
                                render: (_, room) => {
                                    const [status, statusClass] = getStatus(room);
                                    return <Tag className={`online-classroom-table-status ${statusClass}`}>{status}</Tag>;
                                }
                            },
                            {
                                title: "Starts at",
                                dataIndex: "startsAt",
                                key: "startsAt",
                                render: value => formatDateTime(value)
                            },
                            {
                                title: "Ends at",
                                dataIndex: "endsAt",
                                key: "endsAt",
                                render: value => formatDateTime(value)
                            },
                            {
                                title: "Google Meet",
                                dataIndex: "googleMeetUrl",
                                key: "googleMeetUrl",
                                width: 145,
                                render: value => (
                                    <a className="online-classroom-table-join" href={value} target="_blank" rel="noopener noreferrer">
                                        <ExternalLink size={16} /> Join Meet
                                    </a>
                                )
                            },
                            {
                                title: "Actions",
                                key: "actions",
                                width: 130,
                                render: (_, room) => (
                                    <Space size={4}>
                                        <Tooltip title="Edit classroom">
                                            <button type="button" className="online-classroom-table-action edit" onClick={() => edit(room)} aria-label="Edit classroom">
                                                <Pencil size={16} />
                                            </button>
                                        </Tooltip>
                                        <Tooltip title="Delete classroom">
                                            <button type="button" className="online-classroom-table-action delete" onClick={() => remove(room.onlineClassroomId)} aria-label="Delete classroom">
                                                <Trash2 size={16} />
                                            </button>
                                        </Tooltip>
                                    </Space>
                                )
                            }
                        ]}
                    />
                ) : (
                    <div className="online-classroom-grid">
                        {rooms.map((room) => {
                            const [status, statusClass] = getStatus(room);
                            return (
                                <article className="online-classroom-card" key={room.onlineClassroomId}>
                                    <div className="online-classroom-card-top">
                                        <div className="online-classroom-video-icon"><Video size={21} /></div>
                                        <span className={`online-classroom-status ${statusClass}`}>{status}</span>
                                    </div>
                                    <h2>{room.title}</h2>
                                    <p className="online-classroom-class">{room.className}{room.courseName ? ` · ${room.courseName}` : ""}</p>
                                    {!isTeacher && <p className="online-classroom-teacher">Teacher: {room.teacherName}</p>}
                                    <div className="online-classroom-time"><CalendarDays size={16} /><span>{formatDateTime(room.startsAt)} – {formatDateTime(room.endsAt)}</span></div>
                                    <div className="online-classroom-actions">
                                        <a className="online-classroom-join" href={room.googleMeetUrl} target="_blank" rel="noopener noreferrer"><ExternalLink size={16} /> Join Google Meet</a>
                                    </div>
                                </article>
                            );
                        })}
                    </div>
                )}
            </div>

            {isTeacher && showForm && (
                <div className="online-classroom-modal-backdrop" role="presentation">
                    <form className="online-classroom-modal" onSubmit={submit}>
                        <button className="online-classroom-close" type="button" onClick={closeForm} aria-label="Close"><X size={20} /></button>
                        <h2>{editingId ? "Edit online classroom" : "Create online classroom"}</h2>
                        <label>Class<select required value={form.classRoomId} onChange={(event) => setForm({ ...form, classRoomId: event.target.value })}><option value="">Select a class</option>{classes.map((item) => <option key={item.classRoomId} value={item.classRoomId}>{item.className}{item.courseName ? ` — ${item.courseName}` : ""}</option>)}</select></label>
                        <label>Title<input required maxLength="200" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} placeholder="Example: Speaking practice" /></label>
                        <label>Google Meet link<input required type="url" value={form.googleMeetUrl} onChange={(event) => setForm({ ...form, googleMeetUrl: event.target.value })} placeholder="https://meet.google.com/..." /></label>
                        <div className="online-classroom-datetime-row"><label>Start time<input required type="datetime-local" value={form.startsAt} onChange={(event) => updateStartTime(event.target.value)} /></label><label>End time<input required type="datetime-local" min={form.startsAt || undefined} value={form.endsAt} onChange={(event) => setForm({ ...form, endsAt: event.target.value })} /></label></div>
                        <button className="online-classroom-primary" disabled={submitting} type="submit">{submitting ? "Saving..." : editingId ? "Save changes" : "Create classroom"}</button>
                    </form>
                </div>
            )}
        </section>
    );
}

export default OnlineClassroomContent;
