import {
    FileText,
    Headphones,
    Mic,
    BookOpen,
    PenLine
} from "lucide-react";

import "../styles/exerciseStudent.css";

function ExerciseSkillNavbar({
    activeSkill,
    onChange,
    totalExercises = 0
  
}) {
    const skills = [
        {
            id: 1,
            name: "Grammar",
            description: "Structure and foundations",
            icon: FileText
        },
        {
            id: 2,
            name: "Listening",
            description: "Listening practice and response",
            icon: Headphones
        },
        {
            id: 3,
            name: "Speaking",
            description: "Pronunciation and communication",
            icon: Mic
        },
        {
            id: 4,
            name: "Reading",
            description: "Reading comprehension and vocabulary",
            icon: BookOpen
        },
        {
            id: 5,
            name: "Writing",
            description: "Expression and academic writing",
            icon: PenLine
        }
    ];

    const handleChangeSkill = skillId => {
        if (skillId === activeSkill) {
            return;
        }

        onChange(skillId);
    };

    return (
        <section className="exercise-skill-navigation">
            <div className="exercise-skill-navigation-inner">
                <div className="exercise-skill-navigation-heading">
                    <span className="exercise-skill-eyebrow">
                        Practice Program
                    </span>

                    <strong>
                        Choose a skill
                    </strong>
                </div>

                <div className="exercise-skill-tabs-wrapper">
                    <div
                        className="exercise-skill-switcher"
                        role="tablist"
                        aria-label="Exercise skill list"
                    >
                        {skills.map(skill => {
                            const Icon = skill.icon;

                            const isActive =
                                activeSkill === skill.id;

                            return (
                                <button
                                    type="button"
                                    role="tab"
                                    aria-selected={isActive}
                                    key={skill.id}
                                    className={
                                        isActive
                                            ? "exercise-skill-switcher-item active"
                                            : "exercise-skill-switcher-item"
                                    }
                                    onClick={() =>
                                        handleChangeSkill(
                                            skill.id
                                        )
                                    }
                                >
                                    <span className="exercise-skill-switcher-icon">
                                        <Icon size={18} />
                                    </span>

                                    <span className="exercise-skill-switcher-content">
                                        <strong>
                                            {skill.name}
                                        </strong>

                                        <small>
                                            {skill.description}
                                        </small>
                                    </span>

                                    {isActive && (
                                        <span className="exercise-skill-switcher-badge">
                                            {totalExercises} exercises
                                        </span>
                                    )}

                                    <span className="exercise-skill-switcher-indicator" />
                                </button>
                            );
                        })}
                    </div>
                </div>

                
            </div>
        </section>
    );
}

export default ExerciseSkillNavbar;
