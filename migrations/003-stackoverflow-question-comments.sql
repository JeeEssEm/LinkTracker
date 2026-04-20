CREATE TABLE IF NOT EXISTS stackoverflow_question_comments (
    id BIGINT PRIMARY KEY,
    question_id BIGINT NOT NULL REFERENCES stackoverflow_questions(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);
