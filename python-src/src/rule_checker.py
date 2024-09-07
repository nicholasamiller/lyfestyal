import ollama
import argparse
import json

prompt_template = """
Sentence: {sentence}
Rule: {rule}
Reference metadata: {metadata}

Does the sentence satisfy the rule? RESPOND IN JSON FORMAT ONLY:
If yes, respond with {{ "result" : "true" }}
If no, respond with {{ "result" : "false", "comment" : "Suggested improvement and reference metadata"}}
In the text for the suggestion, make reference to the style rule, and the reference metadata.
In the text for the suggestion, do not include anything like "The sentence does not satisfy the rule.", just give the suggestion and the reference.
DO NOT INCLUDE ANY NEWLINE CHARACTERS IN THE JSON RESPONSE.
"""

def check_sentence_for_rule(sentence, rule, metadata):
    # call the ollama API, using the prompt template
    prompt = prompt_template.format(sentence=sentence["text"], rule=rule, metadata=metadata)
    
    response = ollama.chat(model='llama3.1:8b', messages=[{'role': 'user', 'content': prompt}])
    response_content = json.loads(response['message']['content'])
    return response_content


# Arguments:
# 1. A list of sentences with IDs in json format like this: [{"id" : "1a", "text": "This is the text of the sentence."},{"id" : "1b", "text": "Here is the text of another sentence."}]
# 2. The text of a the rule (string)
# 3. Metadata about the rule (string)
def main():
    parser = argparse.ArgumentParser(description='Check if a rule is satisfied by a list of sentences.')
    parser.add_argument('sentences', type=str, help='A list of sentences with IDs in json format like this: [{"id" : "1a", "text": "This is the text of the sentence."},{"id" : "1b", "text": "Here is the text of another sentence."}]')
    parser.add_argument('rule', type=str, help='The text of a the rule (string)')
    parser.add_argument('metadata', type=str, help='Metadata about the rule (string)')

    args = parser.parse_args()

    sentencesfilename = args.sentences
    with open(sentencesfilename, 'r') as sentencesfile:
        sentences = json.load(sentencesfile)

    rule = args.rule
    metadata = args.metadata

    flagged_sentences = []

    for sentence in sentences:
        sentence_response = check_sentence_for_rule(sentence, rule, metadata)
        if sentence_response["result"] == "false":
            flagged_sentences.append({ "id" : sentence["id"],"comment" : sentence_response["comment"] })

    print(json.dumps(flagged_sentences))
        

if __name__ == '__main__':
    main()