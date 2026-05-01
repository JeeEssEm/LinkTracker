CREATE TABLE stackoverflow_questions (
    id bigint NOT NULL,
    link_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT pk_stackoverflow_questions PRIMARY KEY (id),
    CONSTRAINT fk_stackoverflow_questions_links_link_id FOREIGN KEY (link_id) REFERENCES links(id) ON DELETE CASCADE
);

CREATE TABLE stackoverflow_answers (
    id bigint NOT NULL,
    question_id bigint NOT NULL,
    created_at timestamp with time zone DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT pk_stackoverflow_answers PRIMARY KEY (id),
    CONSTRAINT fk_stackoverflow_answers_questions_question_id FOREIGN KEY (question_id) REFERENCES stackoverflow_questions (id) ON DELETE CASCADE
);

CREATE TABLE stackoverflow_comments (
    id bigint NOT NULL,
    answer_id bigint NOT NULL,
    created_at timestamp with time zone DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT pk_stackoverflow_comments PRIMARY KEY (id),
    CONSTRAINT fk_stackoverflow_comments_answers_answer_id FOREIGN KEY (answer_id) REFERENCES stackoverflow_answers (id) ON DELETE CASCADE
);


CREATE INDEX ix_stackoverflow_questions_link_id ON stackoverflow_questions (link_id);
CREATE INDEX ix_stackoverflow_answers_question_id ON stackoverflow_answers (question_id);
CREATE INDEX ix_stackoverflow_comments_answer_id ON stackoverflow_comments (answer_id);
